using LoggerLib;
using System.Collections.Generic;
using LoggerLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EasySave.Models
{
    //Enum for log format
    public class AppSettings
    {
        public string Language { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public LogFormat LogFormat { get; set; } = LogFormat.Json; // Default log format is JSON

        public List<string> ExtensionsToCrypt { get; set; } = new List<string>(); // List of file extensions to encrypt

        public List<string> PriorityExtensions { get; set; } = new List<string>();
      
        public BusinessSoftware BusinessSoftware { get; set; } = new BusinessSoftware();

        public int MaxParallelLargeFileSizeKo { get; set; }
    }

    // Class representing business software details
    public class BusinessSoftware
    {
        public string Name { get; set; } = "Aucun";
        public string FullPath { get; set; } = "";
    }
}