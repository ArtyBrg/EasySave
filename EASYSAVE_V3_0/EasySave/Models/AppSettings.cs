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
        public string Language { get; set; } = "EN";

        [JsonConverter(typeof(StringEnumConverter))]
        public LogFormat LogFormat { get; set; } = LogFormat.Json;

        public List<string> ExtensionsToCrypt { get; set; } = new List<string>();

        public List<string> PriorityExtensions { get; set; } = new List<string>();
      
        public BusinessSoftware BusinessSoftware { get; set; } = new BusinessSoftware();

        public int MaxParallelLargeFileSizeKo { get; set; } = 10000;

    }

    // Enum for log format
    public class BusinessSoftware
    {
        public string Name { get; set; } = "Aucun";
        public string FullPath { get; set; } = "";
    }
}