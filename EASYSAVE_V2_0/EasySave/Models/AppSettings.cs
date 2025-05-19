using LoggerLib;
using System.Collections.Generic;
using LoggerLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EasySave.Models
{
    public class AppSettings
    {
        // Langue de l'application
        public string Language { get; set; } = "EN";

        // Format des logs
        [JsonConverter(typeof(StringEnumConverter))]
        public LogFormat LogFormat { get; set; } = LogFormat.Json;

        // Extensions à crypter
        public List<string> ExtensionsToCrypt { get; set; } = new List<string>();

        // Logiciel métier
        public BusinessSoftware BusinessSoftware { get; set; } = new BusinessSoftware();
    }

    public class BusinessSoftware
    {
        public string Name { get; set; } = "Aucun";
        public string FullPath { get; set; } = "";
    }
}