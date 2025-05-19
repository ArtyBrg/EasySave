namespace EasySave.Models
{
    // Enum for log format
    public class BackupJob
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string SourcePath { get; set; }
        public required string TargetPath { get; set; }
        public bool IsSelected { get; set; }
        public required string Type { get; set; }
        public bool IsEncryptionEnabled { get; set; } = false;
    }
}