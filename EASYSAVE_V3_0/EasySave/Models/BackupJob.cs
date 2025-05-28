using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasySave.Services;
using EasySave.Models;

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

        public List<string> PendingFiles { get; set; } = new List<string>();
    }
}