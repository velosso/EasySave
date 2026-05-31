using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace EasyLog
{
    public class XmlLogger : Ilogger
    {
        private readonly string _logPath;
        
        private readonly XmlSerializer _serializer =
            new XmlSerializer(typeof(List<LogEntry>));

        public XmlLogger()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

            Directory.CreateDirectory(dir);
            _logPath = Path.Combine(dir,
                $"{DateTime.Now:yyyy-MM-dd}.xml");
        }

        public void Log(string jobName,
                        string sourcePath,
                        string targetPath,
                        long   fileSize,
                        long   transferTimeMs,
                        long   encryptionTimeMs = 0)
        {
            List<LogEntry> entries = LoadExisting();
            entries.Add(new LogEntry(jobName, sourcePath, targetPath,
                                     fileSize, transferTimeMs, encryptionTimeMs));
            using StreamWriter writer = new StreamWriter(_logPath);
            _serializer.Serialize(writer, entries);
        }

        private List<LogEntry> LoadExisting()
        {
            if (!File.Exists(_logPath))
                return new List<LogEntry>();

            try
            {
                using StreamReader reader = new StreamReader(_logPath);
                return (List<LogEntry>)_serializer.Deserialize(reader)
                       ?? new List<LogEntry>();
            }
            catch
            {
                return new List<LogEntry>();
            }
        }
    }
}