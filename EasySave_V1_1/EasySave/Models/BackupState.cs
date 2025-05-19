// Models/BackupState.cs
namespace EasySave.Models
{
    public class BackupState
    {
        public string Name { get; set; } = "";
        public string SourcePath { get; set; } = "";
        public string TargetPath { get; set; } = "";
        public string State { get; set; } = "INACTIVE";
        public int TotalFiles { get; set; } = 0;
        public long TotalSize { get; set; } = 0;
        public int FilesRemaining { get; set; } = 0;
        public double Progress { get; set; } = 0;
    }
}