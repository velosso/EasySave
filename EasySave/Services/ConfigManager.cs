using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySave.Models;

namespace EasySave.Services
{
    public static class ConfigManager
    {
        private const int MaxJobs = 5;

        private static readonly JsonSerializerOptions Options =
            new JsonSerializerOptions { WriteIndented = true };

        private static string ConfigPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "EasySave");

                Directory.CreateDirectory(dir);

                return Path.Combine(dir, "jobs.json");
            }
        }

        public static List<WorkSave> LoadJobs()
        {
            if (!File.Exists(ConfigPath))
                return new List<WorkSave>();

            try
            {
                string json = File.ReadAllText(ConfigPath);

                return JsonSerializer.Deserialize<List<WorkSave>>(json)
                       ?? new List<WorkSave>();
            }
            catch (JsonException)
            {
                Console.WriteLine("[WARNING] jobs.json corrupted — starting fresh.");
                return new List<WorkSave>();
            }
        }

        public static void SaveJobs(List<WorkSave> jobs)
        {
            File.WriteAllText(ConfigPath,
                JsonSerializer.Serialize(jobs, Options));
        }

        public static void AddJob(List<WorkSave> jobs, WorkSave newJob)
        {
            if (jobs.Count >= MaxJobs)
                throw new InvalidOperationException(
                    $"Maximum {MaxJobs} jobs allowed in version 1.0.");

            if (string.IsNullOrWhiteSpace(newJob.Name))
                throw new ArgumentException(
                    "Job name cannot be empty.");

            if (newJob.BackupType != "complete"
                && newJob.BackupType != "differentielle")
                throw new ArgumentException(
                    $"Invalid type '{newJob.BackupType}'. " +
                    $"Use 'complete' or 'differentielle'.");

            jobs.Add(newJob);
            SaveJobs(jobs);
        }

        public static void RemoveJob(List<WorkSave> jobs, int index)
        {
            if (index < 0 || index >= jobs.Count)
                throw new IndexOutOfRangeException(
                    $"Index {index + 1} is invalid. " +
                    $"List contains {jobs.Count} job(s).");

            jobs.RemoveAt(index);
            SaveJobs(jobs);
        }
    }
}