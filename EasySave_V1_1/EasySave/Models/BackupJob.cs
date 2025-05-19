// Models/BackupJob.cs
using System;
using System.IO;
using System.Diagnostics;

namespace EasySave.Models
{
    public class BackupJob
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string SourcePath { get; set; }
        public required string TargetPath { get; set; }
        public required string Type { get; set; } // "Complete" or "Differential"
    }
}