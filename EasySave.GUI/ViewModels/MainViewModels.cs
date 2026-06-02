using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EasySave.GUI.Helpers;
using EasySave.GUI.Views;
using EasySave.Models;
using EasySave.Services;
using EasyLog;

namespace EasySave.GUI.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // ===== SERVICES =====
        private readonly StateWriter _stateWriter;
        private BackupEngine         _engine;
        private AppSettings          _settings;

        // ===== PROPRIÉTÉS OBSERVABLES =====

        private ObservableCollection<WorkSave> _jobs;
        public ObservableCollection<WorkSave> Jobs
        {
            get => _jobs;
            set => SetProperty(ref _jobs, value);
        }

        private WorkSave _selectedJob;
        public WorkSave SelectedJob
        {
            get => _selectedJob;
            set => SetProperty(ref _selectedJob, value);
        }

        private string _newName        = "";
        private string _newSourcePath  = "";
        private string _newTargetPath  = "";
        private string _newBackupType  = "complete";

        public string NewName
        {
            get => _newName;
            set => SetProperty(ref _newName, value);
        }
        public string NewSourcePath
        {
            get => _newSourcePath;
            set => SetProperty(ref _newSourcePath, value);
        }
        public string NewTargetPath
        {
            get => _newTargetPath;
            set => SetProperty(ref _newTargetPath, value);
        }
        public string NewBackupType
        {
            get => _newBackupType;
            set => SetProperty(ref _newBackupType, value);
        }

        private string _statusMessage = "Ready — EasySave v2.0";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private double _progress = 0;
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        private bool _isRunning = false;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                SetProperty(ref _isRunning, value);
                OnPropertyChanged(nameof(IsNotRunning));
            }
        }
        public bool IsNotRunning => !_isRunning;

        // ===== COMMANDES =====
        public ICommand AddJobCommand      { get; }
        public ICommand RemoveJobCommand   { get; }
        public ICommand ExecuteJobCommand  { get; }
        public ICommand ExecuteAllCommand  { get; }
        public ICommand OpenSettingsCommand { get; }

        // ===== CONSTRUCTEUR =====
        public MainViewModel()
        {
            _settings    = SettingsManager.Load();
            _stateWriter = new StateWriter();

            RebuildEngine();

            Jobs = new ObservableCollection<WorkSave>(
                ConfigManager.LoadJobs());

            AddJobCommand = new RelayCommand(
                ExecuteAddJob,
                () => IsNotRunning
                      && !string.IsNullOrWhiteSpace(NewName)
                      && !string.IsNullOrWhiteSpace(NewSourcePath)
                      && !string.IsNullOrWhiteSpace(NewTargetPath));

            RemoveJobCommand = new RelayCommand(
                ExecuteRemoveJob,
                () => SelectedJob != null && IsNotRunning);

            ExecuteJobCommand = new RelayCommand(
                ExecuteOneJob,
                () => SelectedJob != null && IsNotRunning);

            ExecuteAllCommand = new RelayCommand(
                ExecuteAllJobs,
                () => Jobs.Count > 0 && IsNotRunning);

            OpenSettingsCommand = new RelayCommand(
                ExecuteOpenSettings,
                () => IsNotRunning);
        }

        // ===== MÉTHODES PRIVÉES =====

        private void RebuildEngine()
        {
            Ilogger logger = SettingsManager.CreateLogger(_settings);
            _engine = new BackupEngine(logger, _stateWriter, _settings);
        }

        private void ExecuteAddJob()
        {
            try
            {
                WorkSave newJob = new WorkSave(
                    NewName, NewSourcePath, NewTargetPath, NewBackupType);

                ConfigManager.AddJob(Jobs.ToList(), newJob);
                Jobs.Add(newJob);

                StatusMessage =
                    $"Job '{NewName}' added ({Jobs.Count} total).";

                NewName       = "";
                NewSourcePath = "";
                NewTargetPath = "";
                NewBackupType = "complete";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show(ex.Message, "EasySave",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExecuteRemoveJob()
        {
            if (SelectedJob == null) return;

            string name   = SelectedJob.Name;
            int    index  = Jobs.IndexOf(SelectedJob);

            Jobs.Remove(SelectedJob);
            ConfigManager.SaveJobs(Jobs.ToList());

            StatusMessage = $"Job '{name}' removed.";
        }

        private async void ExecuteOneJob()
        {
            if (SelectedJob == null || IsRunning) return;

            IsRunning     = true;
            Progress      = 0;
            StatusMessage = $"Running '{SelectedJob.Name}'...";

            WorkSave job = SelectedJob;

            try
            {
                await Task.Run(() => _engine.Execute(job));
                Progress      = 100;
                StatusMessage = $"'{job.Name}' completed ✓";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show(ex.Message, "EasySave",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async void ExecuteAllJobs()
        {
            if (IsRunning) return;

            IsRunning     = true;
            Progress      = 0;
            StatusMessage = $"Running {Jobs.Count} job(s)...";

            var jobs = Jobs.ToList();

            try
            {
                await Task.Run(() => _engine.ExecuteAll(jobs));
                Progress      = 100;
                StatusMessage = $"All {jobs.Count} job(s) completed ✓";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show(ex.Message, "EasySave",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void ExecuteOpenSettings()
        {
            SettingsWindow win = new SettingsWindow(_settings);
            win.ShowDialog();

            if (win.Saved)
            {
                _settings = win.UpdatedSettings;
                SettingsManager.Save(_settings);
                RebuildEngine();
                StatusMessage = "Settings saved.";
            }
        }

        public void OnWindowClosing()
        {
            ConfigManager.SaveJobs(Jobs.ToList());
        }
    }
}