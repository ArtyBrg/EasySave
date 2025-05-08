using System;
using System.IO;

// Gestionnaire d'état

namespace EasySave
{
    public static class StateWriter
    {
        public static void UpdateState(string jobName, string status, double progress)
        {
            try
            {
                File.WriteAllText("state.json",
                    $"{{\"job\": \"{jobName}\", \"status\": \"{status}\", \"progress\": {progress}, \"timestamp\": \"{DateTime.Now:O}\"}}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to update state: {ex.Message}");
            }
        }
    }
}