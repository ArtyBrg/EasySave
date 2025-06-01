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
    
    /// Manages the state of backup jobs
    public class StateService
    {
        private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "Settings", "settings.json");

        // AJOUTEZ CETTE MÉTHODE
        public static List<string> GetPriorityExtensions()
        {
            var settings = Load();
            return settings.PriorityExtensions ?? new List<string>();
        }

        // Ensure the settings directory exists
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

        // Load settings from the JSON file
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

        private static StateService _instance;
        public static StateService Instance => _instance ??= new StateService(new LoggerService());

        private string StateFile = Path.Combine(AppContext.BaseDirectory, "States", "state.json");
        private readonly List<BackupState> _states = new();
        private readonly LoggerService _logger;

        public event EventHandler<BackupState> StateUpdated;

        private StateService(LoggerService logger)
        {
            _logger = logger;
            LoadStates();
        }

        private RemoteConsoleService _remoteConsole;
        public void SetRemoteConsole(RemoteConsoleService remoteConsole)
        {
            _remoteConsole = remoteConsole;
        }

        // Update the state of a backup job
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
                state.TotalFiles = totalFiles > 0 ? totalFiles : state.TotalFiles;
                state.TotalSize = totalSize > 0 ? totalSize : state.TotalSize;
                state.FilesRemaining = filesLeft;
                state.RemainingSize = remainingSize;
                state.CurrentSourceFile = currentSourceFile;
                state.CurrentTargetFile = currentTargetFile;
                state.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                Console.WriteLine("RemoteConsole is null ? " + (_remoteConsole == null));
                if (_remoteConsole != null)
                    _remoteConsole.SendStateUpdate(state);

                SaveStates();

                // Notification in the IU
                StateUpdated?.Invoke(this, state);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update state: {ex.Message}");
            }
        }

        // Get the state of a backup job
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

        // Save the state of a backup job
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

        // Get the state of a backup job
        public IEnumerable<BackupState> GetAllStates() => _states.AsReadOnly();
    }
}