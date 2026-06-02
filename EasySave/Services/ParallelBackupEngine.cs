using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasySave.Models;
using EasyLog;

namespace EasySave.Services
{
    public class ParallelBackupEngine
    {
        private readonly Ilogger     _logger;
        private readonly StateWriter _stateWriter;
        private readonly AppSettings _settings;

        // Sémaphore : 1 seul gros fichier à la fois
        private readonly SemaphoreSlim _largeFileSemaphore
            = new SemaphoreSlim(1, 1);

        // Mutex global CryptoSoft mono-instance
        private static readonly SemaphoreSlim _cryptoSemaphore
            = new SemaphoreSlim(1, 1);

        // Verrou pour les fichiers prioritaires
        private static readonly object _priorityLock = new object();
        private static int _priorityFilesWaiting = 0;

        // Contrôleurs des travaux (Pause/Play/Stop)
        private readonly Dictionary<string, JobController> _controllers
            = new Dictionary<string, JobController>();

        public ParallelBackupEngine(Ilogger logger,
                                     StateWriter  stateWriter,
                                     AppSettings  settings)
        {
            _logger      = logger;
            _stateWriter = stateWriter;
            _settings    = settings ?? new AppSettings();
        }

        // Récupère le contrôleur d'un travail (crée si absent)
        public JobController GetController(string jobName)
        {
            if (!_controllers.ContainsKey(jobName))
                _controllers[jobName] = new JobController(jobName);

            return _controllers[jobName];
        }

        // Lance TOUS les travaux en parallèle
        public async Task ExecuteAllParallelAsync(List<WorkSave> jobs)
        {
            if (jobs.Count == 0) return;

            // Créer un contrôleur pour chaque travail
            foreach (WorkSave job in jobs)
            {
                var ctrl = GetController(job.Name);
                ctrl.Play(); // démarre en état Running
            }

            _stateWriter.InitializeAll(jobs);

            // Démarrer tous les travaux en parallèle
            List<Task> tasks = jobs.Select(job =>
                Task.Run(() => ExecuteJobInternal(job,
                    GetController(job.Name)))
            ).ToList();

            await Task.WhenAll(tasks);
        }

        // Exécute UN travail
        public async Task ExecuteJobAsync(WorkSave job)
        {
            var ctrl = GetController(job.Name);
            ctrl.Play();

            _stateWriter.InitializeAll(new List<WorkSave> { job });

            await Task.Run(() => ExecuteJobInternal(job, ctrl));
        }

        private void ExecuteJobInternal(WorkSave job,
                                         JobController ctrl)
        {
            try
            {
                if (!Directory.Exists(job.SourcePath))
                {
                    ctrl.SetError();
                    return;
                }

                Directory.CreateDirectory(job.TargetPath);

                // Calcul des totaux
                int  totalFiles = CountFiles(job.SourcePath);
                long totalSize  = ComputeSize(job.SourcePath);

                DateTime lastBackup = job.BackupType == "complete"
                    ? DateTime.MinValue
                    : GetLastBackupDate(job.TargetPath);

                CopyDirectoryRecursive(job.Name, ctrl,
                    job.SourcePath, job.TargetPath,
                    job.BackupType == "complete",
                    lastBackup,
                    totalFiles, totalSize,
                    ref totalFiles, totalSize);

                if (!ctrl.CancellationToken.IsCancellationRequested)
                    ctrl.SetCompleted();

                _stateWriter.SetInactive(job.Name);
            }
            catch (OperationCanceledException)
            {
                ctrl.SetError();
                _stateWriter.SetInactive(job.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {job.Name}: {ex.Message}");
                ctrl.SetError();
                _stateWriter.SetInactive(job.Name);
            }
        }

        private void CopyDirectoryRecursive(
            string        jobName,
            JobController ctrl,
            string        source,
            string        target,
            bool          fullBackup,
            DateTime      lastBackup,
            int           totalFiles,
            long          totalSize,
            ref int       remaining,
            long          remainingSize)
        {
            Directory.CreateDirectory(target);

            foreach (string filePath in Directory.GetFiles(source))
            {
                // Vérifier pause/stop AVANT chaque fichier
                if (!ctrl.WaitIfPausedOrCancelled())
                    return;

                // Vérifier logiciel métier → pause automatique
                WaitForBusinessSoftware(ctrl);
                if (ctrl.CancellationToken.IsCancellationRequested)
                    return;

                FileInfo fi = new FileInfo(filePath);

                // Filtre différentiel
                if (!fullBackup && fi.LastWriteTime <= lastBackup)
                    continue;

                // Gestion des fichiers prioritaires
                bool isPriority = IsPriorityFile(filePath);

                if (!isPriority)
                {
                    // Attendre s'il y a des fichiers prioritaires en attente
                    while (_priorityFilesWaiting > 0)
                    {
                        if (ctrl.CancellationToken.IsCancellationRequested)
                            return;
                        Thread.Sleep(100);
                    }
                }
                else
                {
                    Interlocked.Increment(ref _priorityFilesWaiting);
                }

                CopySingleFile(jobName, ctrl, fi,
                    Path.Combine(target, fi.Name),
                    totalFiles, totalSize,
                    ref remaining, remainingSize);

                if (isPriority)
                    Interlocked.Decrement(ref _priorityFilesWaiting);
            }

            foreach (string subDir in Directory.GetDirectories(source))
            {
                if (ctrl.CancellationToken.IsCancellationRequested)
                    return;

                string dirName   = Path.GetFileName(subDir);
                string targetSub = Path.Combine(target, dirName);

                CopyDirectoryRecursive(jobName, ctrl, subDir, targetSub,
                    fullBackup, lastBackup,
                    totalFiles, totalSize,
                    ref remaining, remainingSize);
            }
        }

        private void CopySingleFile(
            string        jobName,
            JobController ctrl,
            FileInfo      fi,
            string        targetFile,
            int           totalFiles,
            long          totalSize,
            ref int       remaining,
            long          remainingSize)
        {
            long timeMs    = -1;
            long encryptMs = 0;
            bool isLarge   = fi.Length > _settings.MaxLargeFileSizeKb * 1024;

            try
            {
                // Gros fichier : attendre le sémaphore
                if (isLarge)
                    _largeFileSemaphore.Wait(ctrl.CancellationToken);

                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    File.Copy(fi.FullName, targetFile, overwrite: true);
                    sw.Stop();
                    timeMs = sw.ElapsedMilliseconds;
                }
                finally
                {
                    if (isLarge)
                        _largeFileSemaphore.Release();
                }

                // Cryptage mono-instance
                var cryptoAdapter = new CryptoSoftAdapter(
                    _settings.CryptoSoftPath);

                if (cryptoAdapter.ShouldEncrypt(fi.FullName,
                        _settings.EncryptedExtensions))
                {
                    _cryptoSemaphore.Wait(ctrl.CancellationToken);
                    try   { encryptMs = cryptoAdapter.Encrypt(targetFile); }
                    finally { _cryptoSemaphore.Release(); }
                }

                remaining--;

                // Mise à jour state
                _stateWriter.UpdateActive(jobName,
                    totalFiles, totalSize,
                    remaining, remainingSize,
                    fi.FullName, targetFile);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"  ✗ {fi.Name} [{ex.Message}]");
            }
            finally
            {
                _logger.Log(jobName, fi.FullName, targetFile,
                            fi.Length, timeMs, encryptMs);
            }
        }

        // Pause automatique si logiciel métier détecté
        private void WaitForBusinessSoftware(JobController ctrl)
        {
            if (string.IsNullOrWhiteSpace(_settings.BusinessSoftware))
                return;

            while (BusinessSoftwareDetector.IsRunning(
                       _settings.BusinessSoftware))
            {
                if (ctrl.CancellationToken.IsCancellationRequested)
                    return;

                Thread.Sleep(500); // vérifie toutes les 500ms
            }
        }

        private bool IsPriorityFile(string filePath)
        {
            if (_settings.PriorityExtensions == null
                || _settings.PriorityExtensions.Count == 0)
                return false;

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return _settings.PriorityExtensions.Contains(ext);
        }

        private int CountFiles(string path)
        {
            int count = Directory.GetFiles(path).Length;
            foreach (string d in Directory.GetDirectories(path))
                count += CountFiles(d);
            return count;
        }

        private long ComputeSize(string path)
        {
            long size = 0;
            foreach (string f in Directory.GetFiles(path))
                size += new FileInfo(f).Length;
            foreach (string d in Directory.GetDirectories(path))
                size += ComputeSize(d);
            return size;
        }

        private DateTime GetLastBackupDate(string targetPath)
        {
            if (!Directory.Exists(targetPath))
                return DateTime.MinValue;

            DateTime latest = DateTime.MinValue;

            foreach (string f in Directory.GetFiles(
                targetPath, "*", SearchOption.AllDirectories))
            {
                DateTime lw = new FileInfo(f).LastWriteTime;
                if (lw > latest) latest = lw;
            }
            return latest;
        }

        // Pause/Play/Stop pour TOUS les travaux
        public void PauseAll()
        {
            foreach (var ctrl in _controllers.Values)
                if (ctrl.Status == JobStatus.Running)
                    ctrl.Pause();
        }

        public void PlayAll()
        {
            foreach (var ctrl in _controllers.Values)
                if (ctrl.Status == JobStatus.Paused)
                    ctrl.Play();
        }

        public void StopAll()
        {
            foreach (var ctrl in _controllers.Values)
                ctrl.Stop();
        }
    }
}