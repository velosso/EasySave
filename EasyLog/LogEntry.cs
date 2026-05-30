using System;

namespace EasyLog
{
    public class LogEntry
    {
        public string Timestamp      { get; set; }
        public string JobName        { get; set; }
        public string SourcePath     { get; set; }
        public string TargetPath     { get; set; }
        public long   FileSize       { get; set; }
        public long   TransferTimeMs { get; set; }

        public LogEntry() { }

        public LogEntry(string jobName,
            string sourcePath,
            string targetPath,
            long   fileSize,
            long   transferTimeMs)
        {
            Timestamp      = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            JobName        = jobName;
            SourcePath     = sourcePath;
            TargetPath     = targetPath;
            FileSize       = fileSize;
            TransferTimeMs = transferTimeMs;
        }
    }
}