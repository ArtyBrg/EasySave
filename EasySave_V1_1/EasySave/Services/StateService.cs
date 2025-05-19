using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EasySave.Models;

namespace EasySave.Services
{
    // Service for managing backup states with loading, saving, and updating backup states
    public class StateService
    {
        // Name of the JSON file where states are persisted
        private const string StateFile = "state.json";
        // List of backup states
        private readonly List<BackupState> _states = new();
        // Logging service to record errors
        private readonly LoggerService _logger;

        // Constructor that initializes the logging service and loads the states
        public StateService(LoggerService logger)
        {
            _logger = logger;
            LoadStates();
        }

        // Property to access the list of backup states
        private void LoadStates()
        {
            try
            {
                // Checks if the state file exists and loads it
                if (File.Exists(StateFile))
                {
                    // Reads the file content and deserializes it into a list of states
                    var json = File.ReadAllText(StateFile);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        // Deserializes the JSON into a list of states
                        var states = JsonSerializer.Deserialize<List<BackupState>>(json);
                        if (states != null)
                        {
                            _states.Clear();
                            _states.AddRange(states);
                        }
                    }
                }
            }
            // Handles exceptions when loading states
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load states: {ex.Message}");
            }
        }

        // Method to save the states to the JSON file
        private void SaveStates()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true }; // Readable JSON
                var json = JsonSerializer.Serialize(_states, options);
                File.WriteAllText(StateFile, json); // Write to file
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save states: {ex.Message}");
            }
        }

        // Updates the state of a backup job or adds it if it does not exist
        public void UpdateState(string jobName, string status, double progress,
                            string sourcePath = "", string targetPath = "",
                            int totalFiles = 0, long totalSize = 0, int filesLeft = 0)
        {
            try
            {
                // Converts the state to a uniform format
                string stateValue = status.ToUpper() switch
                {
                    "ACTIVE" => "ACTIVE",
                    "INPROGRESS" => "ACTIVE",
                    "COMPLETED" => "END",
                    "FAILED" => "ERROR",
                    "CREATED" => "INACTIVE",
                    _ => status.ToUpper()
                };

                // Searches for an existing state for this job
                var state = _states.FirstOrDefault(s => s.Name == jobName);
                if (state == null)
                {
                    state = new BackupState { Name = jobName };
                    _states.Add(state);
                }

                // Updates the information
                state.State = stateValue;
                state.Progress = progress;
                state.SourcePath = string.IsNullOrEmpty(sourcePath) ? state.SourcePath : sourcePath;
                state.TargetPath = string.IsNullOrEmpty(targetPath) ? state.TargetPath : targetPath;
                state.TotalFiles = totalFiles > 0 ? totalFiles : state.TotalFiles;
                state.TotalSize = totalSize > 0 ? totalSize : state.TotalSize;
                state.FilesRemaining = filesLeft;

                // Save after modification
                SaveStates();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update state: {ex.Message}");
            }
        }

        // Returns all backup states as read-only
        public IEnumerable<BackupState> GetAllStates()
        {
            return _states.AsReadOnly();
        }
    }
}
