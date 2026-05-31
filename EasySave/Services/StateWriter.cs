using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySave.Models;

namespace EasySave.Services
{
    public class StateWriter
    {
        private readonly string _statePath;

        private readonly JsonSerializerOptions _options =
            new JsonSerializerOptions { WriteIndented = true };

        public StateWriter()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "EasySave");

            Directory.CreateDirectory(dir);

            _statePath = Path.Combine(dir, "state.json");
        }

        public void InitializeAll(List<WorkSave> jobs)
        {
            var states = new List<BackupState>();

            foreach (WorkSave job in jobs)
                states.Add(BackupState.CreateInactive(job.Name));

            WriteStates(states);
        }

        public void UpdateActive(string jobName,
                                 int    totalFiles,
                                 long   totalSize,
                                 int    remainingFiles,
                                 long   remainingSize,
                                 string currentSource,
                                 string currentTarget)
        {
            List<BackupState> states = ReadCurrentStates();

            BackupState state = states.Find(s => s.Name == jobName);

            if (state == null) return;

            int copied = totalFiles - remainingFiles;

            state.Timestamp         = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            state.Status            = "Active";
            state.TotalFiles        = totalFiles;
            state.TotalSize         = totalSize;
            state.RemainingFiles    = remainingFiles;
            state.RemainingSize     = remainingSize;
            state.Progression       = totalFiles > 0
                                      ? Math.Round(copied * 100.0 / totalFiles, 1)
                                      : 0;
            state.CurrentSourceFile = currentSource;
            state.CurrentTargetFile = currentTarget;

            WriteStates(states);
        }

        public void SetInactive(string jobName)
        {
            List<BackupState> states = ReadCurrentStates();

            BackupState state = states.Find(s => s.Name == jobName);

            if (state == null) return;

            state.Timestamp         = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            state.Status            = "Inactive";
            state.Progression       = 100.0;
            state.RemainingFiles    = 0;
            state.RemainingSize     = 0;
            state.CurrentSourceFile = "";
            state.CurrentTargetFile = "";

            WriteStates(states);
        }

        private List<BackupState> ReadCurrentStates()
        {
            if (!File.Exists(_statePath))
                return new List<BackupState>();

            try
            {
                string json = File.ReadAllText(_statePath);
                return JsonSerializer.Deserialize<List<BackupState>>(json)
                       ?? new List<BackupState>();
            }
            catch (JsonException)
            {
                return new List<BackupState>();
            }
        }

        private void WriteStates(List<BackupState> states)
        {
            File.WriteAllText(_statePath,
                JsonSerializer.Serialize(states, _options));
        }
    }
}