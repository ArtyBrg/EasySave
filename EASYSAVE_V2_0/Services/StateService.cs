using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EasySave.Models;

namespace EasySave.Services
{
    public class StateService
    {
        private string StateFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\\..\\..\\", "state.json"));
        private readonly List<BackupState> _states = new();
        private readonly LoggerService _logger;

        public event EventHandler<BackupState> StateUpdated;

        public StateService(LoggerService logger)
        {
            _logger = logger;
            LoadStates();
        }

        public void UpdateState(string jobName, string status, double progress,
                                string sourcePath = "", string targetPath = "",
                                int totalFiles = 0, long totalSize = 0, int filesLeft = 0,
                                long remainingSize = 0,
                                string currentSourceFile = "", string currentTargetFile = "")
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
                Thread.Sleep(30000);
                state.TotalFiles = totalFiles > 0 ? totalFiles : state.TotalFiles;
                state.TotalSize = totalSize > 0 ? totalSize : state.TotalSize;
                state.FilesRemaining = filesLeft;
                state.RemainingSize = remainingSize;
                state.CurrentSourceFile = currentSourceFile;
                state.CurrentTargetFile = currentTargetFile;
                state.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                SaveStates();

                // Notification in the IU
                StateUpdated?.Invoke(this, state);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update state: {ex.Message}");
            }
        }

        private void LoadStates()
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
                _logger.LogError($"Failed to load states: {ex.Message}");
            }
        }

        private void SaveStates()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_states, options);
                File.WriteAllText(StateFile, json);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save states: {ex.Message}");
            }
        }

        public IEnumerable<BackupState> GetAllStates() => _states.AsReadOnly();
    }
}