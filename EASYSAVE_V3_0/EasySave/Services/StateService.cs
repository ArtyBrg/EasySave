using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using EasySave.Models;
using System.Threading.Tasks;
using EasySave.Models;
using System.Text.Json.Serialization;

namespace EasySave.Services
{
    // Manages the state of backup jobs
    public class StateService
    {
        // Path to the settings JSON file
        private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "Settings", "settings.json");
        
        // Returns the list of priority file extensions from settings
        public static List<string> GetPriorityExtensions()
        {
            var settings = Load();
            return settings.PriorityExtensions ?? new List<string>();
        }

        // Saves the AppSettings object to the settings file in JSON format
        public static void Save(AppSettings settings)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SettingsPath, json);
        }

        // Loads the AppSettings object from the settings file, or returns a new one if the file does not exist
        public static AppSettings Load()
        {
            // Check if the settings file exists, if not return default settings
            if (!File.Exists(SettingsPath))
                return new AppSettings();
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            }) ?? new AppSettings();
        }

        // Singleton instance of StateService
        private static StateService _instance;
        public static StateService Instance => _instance ??= new StateService(new LoggerService());

        // Path to the state JSON file
        private string StateFile = Path.Combine(AppContext.BaseDirectory, "States", "state.json");
      
        // List of all backup states
        private readonly List<BackupState> _states = new();
        // Logger for error and info messages
        private readonly LoggerService _logger;

        // Event triggered when a state is updated
        public event EventHandler<BackupState> StateUpdated;

        // Constructor initializes logger and loads states from file
        private StateService(LoggerService logger)
        {
            _logger = logger;
            LoadStates();
        }

        // Reference to the remote console for state updates
        private RemoteConsoleService _remoteConsole;
        public void SetRemoteConsole(RemoteConsoleService remoteConsole)
        {
            _remoteConsole = remoteConsole;
        }

        // Updates the state of a backup job and notifies listeners
        public void UpdateState(string jobName, string status, double progress,
                                string sourcePath = "", string targetPath = "",
                                int totalFiles = 0, long totalSize = 0, int filesLeft = 0,
                                long remainingSize = 0,
                                string currentSourceFile = "", string currentTargetFile = "")
        {
            try
            {
                // Map status to internal state value
                string stateValue = status.ToUpper() switch
                {
                    "ACTIVE" => "ACTIVE",
                    "INPROGRESS" => "ACTIVE",
                    "COMPLETED" => "END",
                    "FAILED" => "ERROR",
                    "CREATED" => "INACTIVE",
                    _ => status.ToUpper()
                };

                // Find or create the backup state for the job
                var state = _states.FirstOrDefault(s => s.Name == jobName);
                if (state == null)
                {
                    state = new BackupState { Name = jobName };
                    _states.Add(state);
                }

                // Update all state properties
                state.State = stateValue;
                state.Progress = progress;
                state.SourcePath = string.IsNullOrEmpty(sourcePath) ? state.SourcePath : sourcePath;
                state.TargetPath = string.IsNullOrEmpty(targetPath) ? state.TargetPath : targetPath;
                state.TotalFiles = totalFiles > 0 ? totalFiles : state.TotalFiles;
                state.TotalSize = totalSize > 0 ? totalSize : state.TotalSize;
                state.FilesRemaining = filesLeft;
                state.RemainingSize = remainingSize;
                state.CurrentSourceFile = currentSourceFile;
                state.CurrentTargetFile = currentTargetFile;
                state.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Send state update to remote console if available
                Console.WriteLine("RemoteConsole is null ? " + (_remoteConsole == null));
                if (_remoteConsole != null)
                    _remoteConsole.SendStateUpdate(state);

                // Save all states to file
                SaveStates();

                // Notify UI or listeners about the state update
                StateUpdated?.Invoke(this, state);
            }
            catch (Exception ex)
            {
                // Log any errors during state update
                _logger.LogError($"Failed to update state: {ex.Message}");
            }
        }

        // Loads all backup states from the state file
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
                // Log any errors during state loading
                _logger.LogError($"Failed to load states: {ex.Message}");
            }
        }

        // Saves all backup states to the state file
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
                // Log any errors during state saving
                _logger.LogError($"Failed to save states: {ex.Message}");
            }
        }

        // Returns a read-only list of all backup states
        public IEnumerable<BackupState> GetAllStates() => _states.AsReadOnly();
    }
}
