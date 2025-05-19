// Models/JsonLogEntry.cs
namespace EasySave.Models
{
    public class JsonLogEntry
    {
        public string Name { get; set; }
        public string FileSource { get; set; }
        public string FileTarget { get; set; }
        public long FileSize { get; set; }
        public double FileTransferTime { get; set; }
        public string Time { get; set; }
    }
}