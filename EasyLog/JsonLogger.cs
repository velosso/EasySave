using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasyLog
{
    public class JsonLogger : Ilogger
    {
        private readonly string           _logPath;
        private readonly List<LogEntry>   _entries;
        private readonly JsonSerializerOptions _options;

        public JsonLogger()
        {
            // On construit le chemin du fichier log
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave",
                "Logs"
            );

            Directory.CreateDirectory(logDir);

            _logPath = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.json");

            _options = new JsonSerializerOptions { WriteIndented = true };

            _entries = LoadExistingEntries();
        }

        public void Log(string jobName,
            string sourcePath,
            string targetPath,
            long   fileSize,
            long   transferTimeMs)
        {
            _entries.Add(new LogEntry(jobName, sourcePath, targetPath,
                fileSize, transferTimeMs));

            File.WriteAllText(_logPath,
                JsonSerializer.Serialize(_entries, _options));
        }

        private List<LogEntry> LoadExistingEntries()
        {
            if (!File.Exists(_logPath))
                return new List<LogEntry>();

            try
            {
                string json = File.ReadAllText(_logPath);
                return JsonSerializer.Deserialize<List<LogEntry>>(json)
                       ?? new List<LogEntry>();
            }
            catch (JsonException)
            {
                return new List<LogEntry>();
            }
        }
    }
}