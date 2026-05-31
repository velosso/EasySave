namespace EasySave.Models
{
    public class WorkSave
    {
        public string Name       { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public string BackupType { get; set; }

        public WorkSave() { }

        public WorkSave(string name,
            string sourcePath,
            string targetPath,
            string backupType)
        {
            Name = name;
            SourcePath = sourcePath;
            TargetPath = targetPath;
            BackupType = backupType;
        }

        public override string ToString()
        {
            return $"[{Name}] {SourcePath} → {TargetPath} ({BackupType})";
        }
    }
}