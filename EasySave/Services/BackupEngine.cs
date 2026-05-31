using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using EasySave.Models;
using EasyLog;

namespace EasySave.Services
{
    public class BackupEngine
    {
        private readonly Ilogger _logger;
        private readonly StateWriter         _stateWriter;
        private readonly AppSettings         _settings;
        private readonly CryptoSoftAdapter   _cryptoAdapter;

        private int  _totalFiles;
        private long _totalSize;
        private int  _remainingFiles;
        private long _remainingSize;

        public BackupEngine(Ilogger logger,
            StateWriter  stateWriter,
            AppSettings  settings = null)
        {
            _logger = logger;
            _stateWriter = stateWriter;
            _settings    = settings ?? new AppSettings();
            _cryptoAdapter = new CryptoSoftAdapter(_settings.CryptoSoftPath);
        }
        
        public void Execute(WorkSave job)
        {
            // Vérification 1 : logiciel métier actif ?
            if (BusinessSoftwareDetector.IsRunning(_settings.BusinessSoftware))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"  [BLOCKED] '{_settings.BusinessSoftware}' is running. " +
                    $"Cannot start '{job.Name}'.");
                Console.ResetColor();
                return;
            }

            // Vérification 2 : dossier source accessible ?
            if (!Directory.Exists(job.SourcePath))
                throw new DirectoryNotFoundException(
                    $"Source folder not found: '{job.SourcePath}'");

            Directory.CreateDirectory(job.TargetPath);

            ComputeTotals(job.SourcePath);

            Console.WriteLine();
            Console.WriteLine($"  Starting: {job.Name}");
            Console.WriteLine(
                $"  {_totalFiles} file(s) — {FormatSize(_totalSize)}");
            Console.WriteLine(new string('─', 60));

            if (job.BackupType == "complete")
            {
                CopyRecursive(job.Name,
                    job.SourcePath,
                    job.TargetPath,
                    fullBackup: true,
                    lastBackup: DateTime.MinValue);
            }
            else
            {
                DateTime lastBackup = GetLastBackupDate(job.TargetPath);

                CopyRecursive(job.Name,
                    job.SourcePath,
                    job.TargetPath,
                    fullBackup: false,
                    lastBackup: lastBackup);
            }

            _stateWriter.SetInactive(job.Name);

            Console.WriteLine(new string('─', 60));
            Console.WriteLine($"  Done: {job.Name} ✓");
        } 
        
        public void ExecuteAll(List<WorkSave> jobs)
        {
            if (jobs.Count == 0)
            {
                Console.WriteLine("No backup jobs configured.");
                return;
            }

            _stateWriter.InitializeAll(jobs);

            int success = 0;
            int failed  = 0;

            foreach (WorkSave job in jobs)
            {
                try
                {
                    Execute(job);
                    success++;
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [ERROR] {job.Name}: {ex.Message}");
                    Console.ResetColor();
                    _stateWriter.SetInactive(job.Name);
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Summary: {success} succeeded, {failed} failed.");
        }

        public void ExecuteSelection(List<WorkSave> jobs, List<int> indices)
        {
            var selection = new List<WorkSave>();

            foreach (int idx in indices)
            {
                if (idx < 1 || idx > jobs.Count)
                {
                    Console.WriteLine($"[WARNING] Index {idx} is invalid — skipped.");
                    continue;
                }
                selection.Add(jobs[idx - 1]);
            }

            if (selection.Count == 0)
            {
                Console.WriteLine("No valid job to execute.");
                return;
            }

            ExecuteAll(selection);
        }
        
        private void CopyRecursive(string jobName,
                            string source,
                            string target,
                            bool   fullBackup,
                            DateTime lastBackup)
{
    Directory.CreateDirectory(target);

    foreach (string filePath in Directory.GetFiles(source))
    {
        FileInfo fi = new FileInfo(filePath);

        if (!fullBackup && fi.LastWriteTime <= lastBackup)
        {
            _remainingFiles--;
            _remainingSize -= fi.Length;
            continue;
        }

        CopySingleFile(jobName, filePath, target);
    }

    foreach (string subDir in Directory.GetDirectories(source))
    {
        string dirName    = Path.GetFileName(subDir);
        string targetSub  = Path.Combine(target, dirName);

        CopyRecursive(jobName, subDir, targetSub, fullBackup, lastBackup);
    }
}

    private void CopySingleFile(string jobName,
                             string filePath,
                             string targetDir)
{
    FileInfo fi          = new FileInfo(filePath);
    string   targetFile  = Path.Combine(targetDir, fi.Name);
    long     timeMs      = -1;
    long     encryptMs   = 0;

    try
    {
       
        Stopwatch sw = Stopwatch.StartNew();
        File.Copy(filePath, targetFile, overwrite: true);
        sw.Stop();
        timeMs = sw.ElapsedMilliseconds;

       
        if (_cryptoAdapter.ShouldEncrypt(filePath,
                _settings.EncryptedExtensions))
        {
            encryptMs = _cryptoAdapter.Encrypt(targetFile);

            if (encryptMs < 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"  ⚠ Encryption failed on {fi.Name} " +
                    $"(code: {encryptMs})");
                Console.ResetColor();
            }
        }

        
        _remainingFiles--;
        _remainingSize -= fi.Length;

       
        _stateWriter.UpdateActive(
            jobName,
            _totalFiles,      _totalSize,
            _remainingFiles,  _remainingSize,
            filePath,         targetFile);

       
        string encryptInfo = encryptMs > 0
            ? $"   {encryptMs}ms"
            : "";

        Console.WriteLine(
            $"  ✓ {fi.Name,-30} " +
            $"{FormatSize(fi.Length),8}   " +
            $"{timeMs,5}ms{encryptInfo}");
    }
    catch (UnauthorizedAccessException)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(
            $"  ✗ {fi.Name,-30} [access denied]");
        Console.ResetColor();
    }
    catch (IOException ex)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(
            $"  ✗ {fi.Name,-30} [I/O: {ex.Message}]");
        Console.ResetColor();
    }
    finally
    {
       
        _logger.Log(jobName,
                    filePath,
                    targetFile,
                    fi.Length,
                    timeMs,
                    encryptMs);
    }
}

private void ComputeTotals(string path)
{
    _totalFiles     = 0;
    _totalSize      = 0;

    CountRecursive(path);

    _remainingFiles = _totalFiles;
    _remainingSize  = _totalSize;
}

private void CountRecursive(string path)
{
    foreach (string f in Directory.GetFiles(path))
    {
        _totalFiles++;
        _totalSize += new FileInfo(f).Length;
    }

    foreach (string d in Directory.GetDirectories(path))
        CountRecursive(d);
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
        if (lw > latest)
            latest = lw;
    }

    return latest;
}

private string FormatSize(long bytes)
{
    if (bytes < 1024)
        return $"{bytes} B";
    if (bytes < 1024 * 1024)
        return $"{bytes / 1024.0:F1} KB";
    if (bytes < 1024L * 1024 * 1024)
        return $"{bytes / (1024.0 * 1024):F1} MB";

    return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
}
    }
}