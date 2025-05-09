using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

// Gestionnaire d'état

namespace EasySave
{
    // Gestionnaire d'état
    public static class StateWriter
    {
        private const string StateFile = "state.json";
        private static readonly List<BackupState> _states = new();

        static StateWriter()
        {
            LoadStates();
        }

        private static void LoadStates()
        {
            try
            {
                if (File.Exists(StateFile))
                {
                    var json = File.ReadAllText(StateFile);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var states = JsonSerializer.Deserialize<List<BackupState>>(json);
                        if (states != null)
                        {
                            _states.Clear();
                            _states.AddRange(states);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load states: {ex.Message}");
            }
        }

        private static void SaveStates()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_states, options);
                File.WriteAllText(StateFile, json);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to save states: {ex.Message}");
            }
        }

        public static void UpdateState(string jobName, string status, double progress,
                                    string sourcePath = "", string targetPath = "",
                                    int totalFiles = 0, long totalSize = 0, int filesLeft = 0)
        {
            try
            {
                string stateValue = status.ToUpper() switch
                {
                    "ACTIVE" => "ACTIVE",
                    "INPROGRESS" => "ACTIVE",
                    "COMPLETED" => "END",
                    "FAILED" => "ERROR",
                    "CREATED" => "INACTIVE",
                    _ => status.ToUpper()
                };

                var state = _states.FirstOrDefault(s => s.Name == jobName);
                if (state == null)
                {
                    state = new BackupState { Name = jobName };
                    _states.Add(state);
                }

                state.State = stateValue;
                state.Progress = progress;
                state.SourcePath = string.IsNullOrEmpty(sourcePath) ? state.SourcePath : sourcePath;
                state.TargetPath = string.IsNullOrEmpty(targetPath) ? state.TargetPath : targetPath;
                state.TotalFiles = totalFiles > 0 ? totalFiles : state.TotalFiles;
                state.TotalSize = totalSize > 0 ? totalSize : state.TotalSize;
                state.FilesRemaining = filesLeft;

                SaveStates();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to update state: {ex.Message}");
            }
        }

        private class BackupState
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

}