using System;

namespace EasySave.Models
{
    public class BackupState
    {
        public string Name              { get; set; }
        public string Timestamp         { get; set; }
        public string Status            { get; set; }
        public int    TotalFiles        { get; set; }
        public long   TotalSize         { get; set; }
        public int    RemainingFiles    { get; set; }
        public long   RemainingSize     { get; set; }
        public double Progression       { get; set; }
        public string CurrentSourceFile { get; set; }
        public string CurrentTargetFile { get; set; }

        public BackupState() { }

        public static BackupState CreateInactive(string name)
        {
            return new BackupState
            {
                Name = name,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = "Inactive"
            };
        }
    }
}