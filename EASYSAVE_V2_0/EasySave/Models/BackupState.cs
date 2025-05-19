namespace EasySave.Models
{
    public class BackupState
    {
        public string Name { get; set; }
        public string Timestamp { get; set; }
        public string State { get; set; }

        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public double Progress { get; set; }

        public int FilesRemaining { get; set; }
        public long RemainingSize { get; set; }

        public string CurrentSourceFile { get; set; }
        public string CurrentTargetFile { get; set; }

        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
    }
}
