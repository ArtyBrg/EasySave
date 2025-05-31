using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using EasySave.Services;
using LoggerLib;
using System.Text;
using System.Timers;
using System.Windows.Threading;
using EasySave_WPF;

namespace EasySave.ViewModels
{
    // ViewModel for the settings window
    public class SettingsViewModel : ViewModelBase
    {
        // Popup window for business software detection
        private Window _businessPopupWindow;

        // Logger service for logging messages and errors
        private readonly LoggerService _loggerService;

        // Timer to check if business software is running
        private System.Timers.Timer _businessSoftwareCheckTimer;
        private bool _popupAlreadyShown = false;

        // StringBuilder for accumulating log messages
        private StringBuilder _logBuilder = new StringBuilder();
        // Stores the current log content as a string
        private string _logContent = string.Empty;

        // Currently selected language
        private string _selectedLanguage;
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value;
                OnPropertyChanged();
            }
        }

        // Checks if the business software is running and pauses/resumes jobs accordingly
        private void CheckBusinessSoftwareRunning(object sender, ElapsedEventArgs e)
        {
            try
            {
                var settings = SettingsService.Load();

                if (string.IsNullOrEmpty(settings?.BusinessSoftware?.FullPath))
                    return;

                var processName = Path.GetFileNameWithoutExtension(settings.BusinessSoftware.FullPath);
                var runningInstances = Process.GetProcessesByName(processName);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (runningInstances.Any())
                    {
                        if (_businessPopupWindow == null)
                        {
                            _businessPopupWindow = new Views.BusinessSoftwarePopup(this)
                            {
                                Owner = Application.Current.MainWindow,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            };

                            // Disable the main window and show popup
                            Application.Current.MainWindow.IsEnabled = false;
                            _businessPopupWindow.Show();

                            _loggerService?.Log("Job software detected. Automatic pause of jobs...");
                            // Pause all running jobs
                            foreach (var job in App.AppViewModel.ActiveBackupJobs)
                            {
                                if (job.IsRunning && !job.IsPaused)
                                {
                                    job.PauseJob();
                                    _loggerService?.Log($"Job \"{job.Name}\" paused automatically.");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_businessPopupWindow != null)
                        {
                            _businessPopupWindow.Close();
                            _businessPopupWindow = null;

                            Application.Current.MainWindow.IsEnabled = true;

                            // Bring main window to front
                            Application.Current.MainWindow.Activate();
                            Application.Current.MainWindow.Topmost = true;
                            Application.Current.MainWindow.Topmost = false;
                            Application.Current.MainWindow.Focus();

                            // Resume all paused jobs
                            _loggerService?.Log("Closed business software. Automatic resumption of jobs...");
                            foreach (var job in App.AppViewModel.ActiveBackupJobs)
                            {
                                if (job.IsPaused)
                                {
                                    job.PauseJob(); // Toggle to resume
                                    _loggerService?.Log($"Job \"{job.Name}\" resumed automatically.");
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _loggerService?.LogError($"Erreur pendant la détection du logiciel métier : {ex.Message}");
            }
        }

        // List of available log formats
        public ObservableCollection<string> LogFormats { get; } = new ObservableCollection<string> { "JSON", "XML" };
        // Currently selected log format
        private LogFormat _selectedLogFormat;
        public LogFormat SelectedLogFormat
        {
            get => _selectedLogFormat;
            set
            {
                _selectedLogFormat = value;
                OnPropertyChanged();

                LogFormat format = value;
                _loggerService.SetLogFormat(format);
            }
        }

        // List of priority file extensions
        private ObservableCollection<string> _priorityFiles = new ObservableCollection<string>();
        public ObservableCollection<string> PriorityFiles
        {
            get => _priorityFiles;
            set
            {
                _priorityFiles = value;
                OnPropertyChanged();
            }
        }

        // File extension to add to the priority list
        private string _fileToPrioritize = "";
        public string FileToPrioritize
        {
            get => _fileToPrioritize;
            set
            {
                if (_fileToPrioritize != value)
                {
                    _fileToPrioritize = value;
                    OnPropertyChanged();
                }
            }
        }

        // File extension to add to the encryption list
        private string _fileExtensionToCrypt = "";
        public string FileExtensionToCrypt
        {
            get => _fileExtensionToCrypt;
            set
            {
                if (_fileExtensionToCrypt != value)
                {
                    _fileExtensionToCrypt = value;
                    OnPropertyChanged();
                }
            }
        }

        // List of file extensions to encrypt
        private ObservableCollection<string> _extensionsToCrypt = new ObservableCollection<string>();
        public ObservableCollection<string> ExtensionsToCrypt
        {
            get => _extensionsToCrypt;
            set
            {
                _extensionsToCrypt = value;
                OnPropertyChanged();
            }
        }

        // List of available business software processes
        private ObservableCollection<ProcessInfo> _availableProcesses = new ObservableCollection<ProcessInfo>();
        public ObservableCollection<ProcessInfo> AvailableProcesses
        {
            get => _availableProcesses;
            set
            {
                _availableProcesses = value;
                OnPropertyChanged();
            }
        }

        // Currently selected business software
        private ProcessInfo _selectedBusinessSoftware;
        public ProcessInfo SelectedBusinessSoftware
        {
            get => _selectedBusinessSoftware;
            set
            {
                if (_selectedBusinessSoftware != value)
                {
                    _selectedBusinessSoftware = value;
                    CurrentBusinessSoftware = value?.Name ?? "Aucun";
                    OnPropertyChanged();
                }
            }
        }

        // Name of the current business software
        private string _currentBusinessSoftware = "Aucun";
        public string CurrentBusinessSoftware
        {
            get => _currentBusinessSoftware;
            set
            {
                if (_currentBusinessSoftware != value)
                {
                    _currentBusinessSoftware = value;
                    OnPropertyChanged();
                }
            }
        }

        // Commands for UI actions
        public ICommand SetLanguageCommand { get; private set; }
        public ICommand AddExtensionCommand { get; private set; }
        public ICommand RemoveExtensionCommand { get; private set; }
        public ICommand BrowseBusinessSoftwareCommand { get; private set; }
        public ICommand UseCalculatorCommand { get; private set; }
        public ICommand ApplySettingsCommand { get; private set; }
        public ICommand AddPriorityFileCommand { get; private set; }
        public ICommand RemovePriorityFileCommand { get; private set; }

        // Constructor initializes logger, loads settings, and sets up commands and timers
        public SettingsViewModel(LoggerService loggerService)
        {
            _loggerService = loggerService;

            _loggerService.LogMessageAdded += OnLogMessageAdded;

            _loggerService?.Log("SettingsViewModel initialized - Log system ready");

            SetLanguageCommand = new RelayCommand<string>(SetLanguage);
            AddExtensionCommand = new RelayCommand<object>(AddExtension);
            RemoveExtensionCommand = new RelayCommand<string>(RemoveExtension);
            BrowseBusinessSoftwareCommand = new RelayCommand<object>(BrowseBusinessSoftware);
            UseCalculatorCommand = new RelayCommand<object>(UseCalculator);
            ApplySettingsCommand = new RelayCommand(ApplySettings);
            AddPriorityFileCommand = new RelayCommand<object>(AddPriorityFile);
            RemovePriorityFileCommand = new RelayCommand<string>(RemovePriorityFile);

            LoadRunningProcesses();
            LoadSettings();

            _businessSoftwareCheckTimer = new System.Timers.Timer(2000); // 2 seconds
            _businessSoftwareCheckTimer.Elapsed += CheckBusinessSoftwareRunning;
            _businessSoftwareCheckTimer.Start();
        }

        // Handles new log messages and updates the log content
        private void OnLogMessageAdded(object sender, string message)
        {
            _logBuilder.AppendLine(message);
            LogContent = _logBuilder.ToString();
        }

        // Log content to display in the UI
        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        // Sets the selected language and logs the change
        private void SetLanguage(string language)
        {
            SelectedLanguage = language;
            _loggerService?.Log($"Language setting changed to {language}");
        }

        // Adds a file extension to the priority list
        private void AddPriorityFile(object parameter)
        {
            if (string.IsNullOrWhiteSpace(FileToPrioritize))
                return;

            string priority = FileToPrioritize.Trim();
            if (!priority.StartsWith("."))
                priority = "." + priority;

            if (!PriorityFiles.Contains(priority))
            {
                PriorityFiles.Add(priority);
                _loggerService?.Log($"priority added to encryption list: {priority}");
            }

            FileToPrioritize = "";
        }

        // Removes a file extension from the priority list
        private void RemovePriorityFile(string priority)
        {
            if (priority != null)
            {
                PriorityFiles.Remove(priority);
                _loggerService?.Log($"Priority removed from priority list: {priority}");
            }
        }

        // Adds a file extension to the encryption list
        private void AddExtension(object parameter)
        {
            if (string.IsNullOrWhiteSpace(FileExtensionToCrypt))
                return;

            string extension = FileExtensionToCrypt.Trim();
            if (!extension.StartsWith("."))
                extension = "." + extension;

            if (!ExtensionsToCrypt.Contains(extension))
            {
                ExtensionsToCrypt.Add(extension);
                _loggerService?.Log($"Extension added to encryption list: {extension}");
            }

            FileExtensionToCrypt = "";
        }

        // Removes a file extension from the encryption list
        private void RemoveExtension(string extension)
        {
            if (extension != null)
            {
                ExtensionsToCrypt.Remove(extension);
                _loggerService?.Log($"Extension removed from encryption list: {extension}");
            }
        }

        // Opens a file dialog to select the business software
        private void BrowseBusinessSoftware(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Applications exécutables (*.exe)|*.exe",
                Title = "Sélectionner un logiciel métier"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = Path.GetFileName(openFileDialog.FileName);

                var processInfo = new ProcessInfo
                {
                    Name = fileName,
                    FullPath = openFileDialog.FileName
                };

                if (!AvailableProcesses.Any(p => p.FullPath == processInfo.FullPath))
                {
                    AvailableProcesses.Add(processInfo);
                }

                SelectedBusinessSoftware = processInfo;
                _loggerService?.Log($"Business software selected: {fileName}");
            }
        }

        // Sets the calculator as the business software
        private void UseCalculator(object parameter)
        {
            const string calculatorName = "CalculatorApp";

            var calculator = AvailableProcesses.FirstOrDefault(p => p.Name.Equals(calculatorName, StringComparison.OrdinalIgnoreCase));

            if (calculator == null)
            {
                calculator = new ProcessInfo
                {
                    Name = calculatorName,
                    FullPath = "CalculatorApp"
                };
                AvailableProcesses.Add(calculator);
            }

            SelectedBusinessSoftware = calculator;
            _loggerService?.Log("Calculator set as business software");
        }

        // Applies and saves the settings
        private void ApplySettings(object parameter)
        {
            SaveSettings();

            _loggerService?.Log($"Applying settings:");
            _loggerService?.Log($"- Language: {SelectedLanguage}");
            _loggerService?.Log($"- Log format: {SelectedLogFormat}");
            _loggerService?.Log($"- Extensions to encrypt: {string.Join(", ", ExtensionsToCrypt)}");
            _loggerService?.Log($"- Business software: {CurrentBusinessSoftware}");
            _loggerService?.Log("Settings applied successfully.");

            App.AppViewModel.ChangeLanguages(SelectedLanguage);

            MessageBox.Show("Paramètres enregistrés avec succès !", "EasySave", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Loads the list of running processes for business software selection
        private void LoadRunningProcesses()
        {
            try
            {
                AvailableProcesses.Clear();
                var processes = Process.GetProcesses();

                foreach (var process in processes)
                {
                    try
                    {
                        string path = GetProcessFilePath(process);

                        // Only include processes from Program Files directories
                        if (path.StartsWith("C:\\Program Files") || path.StartsWith("C:\\Program Files (x86)"))
                        {
                            AvailableProcesses.Add(new ProcessInfo
                            {
                                Name = process.ProcessName,
                                FullPath = path
                            });
                        }
                    }
                    catch
                    {
                        // Ignore processes that cannot be accessed
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des processus : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggerService?.LogError($"Error loading processes: {ex.Message}");
            }
        }

        // Gets the file path of a process
        private string GetProcessFilePath(Process process)
        {
            try
            {
                return process.MainModule?.FileName ?? process.ProcessName + ".exe";
            }
            catch
            {
                return process.ProcessName + ".exe";
            }
        }

        // Loads settings from the JSON file and applies them
        private void LoadSettings()
        {
            try
            {
                var settings = SettingsService.Load();

                if (settings != null)
                {
                    SelectedLanguage = settings.Language;
                    _loggerService?.Log($"Language loaded from settings: {SelectedLanguage}");
                    SelectedLogFormat = settings.LogFormat;
                    _loggerService?.Log($"Log format loaded from settings: {SelectedLogFormat}");

                    // Load extensions to encrypt
                    ExtensionsToCrypt.Clear();
                    int i = 0;
                    foreach (var extension in settings.ExtensionsToCrypt)
                    {
                        i += 1;
                        ExtensionsToCrypt.Add(extension);
                        _loggerService?.Log($"Extension to crypt number {i} loaded from settings: {extension}");
                    }

                    PriorityFiles.Clear();
                    // Load priority files
                    foreach (var priority in settings.PriorityExtensions)
                    {
                        PriorityFiles.Add(priority);
                        _loggerService?.Log($"Priority file loaded from settings: {priority}");
                    }

                    // Load business software
                    if (!string.IsNullOrEmpty(settings.BusinessSoftware.Name) && settings.BusinessSoftware.Name != "Aucun")
                    {
                        CurrentBusinessSoftware = settings.BusinessSoftware.Name;

                        var businessSoftware = new ProcessInfo
                        {
                            Name = settings.BusinessSoftware.Name,
                            FullPath = settings.BusinessSoftware.FullPath
                        };

                        if (!AvailableProcesses.Any(p => p.FullPath == businessSoftware.FullPath))
                        {
                            AvailableProcesses.Add(businessSoftware);
                        }

                        SelectedBusinessSoftware = businessSoftware;
                        _loggerService?.Log($"Business software loaded from settings: {SelectedBusinessSoftware}");
                    }

                    // Set log format in logger
                    if (_loggerService != null)
                    {
                        try
                        {
                            LogFormat format = settings.LogFormat;
                            _loggerService.SetLogFormat(format);
                        }
                        catch (Exception ex)
                        {
                            _loggerService.LogError($"Failed to set log format from settings: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // If settings file not found, create default settings
                    ExtensionsToCrypt.Add(".txt");
                    ExtensionsToCrypt.Add(".docx");
                    ExtensionsToCrypt.Add(".pdf");

                    PriorityFiles.Add(".txt");
                    PriorityFiles.Add(".docx");
                    PriorityFiles.Add(".pdf");

                    SaveSettings();

                    _loggerService?.Log("Created default settings file");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des paramètres : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggerService?.LogError($"Error loading settings: {ex.Message}");

                // Load default extensions if error occurs
                PriorityFiles.Add(".txt");
                PriorityFiles.Add(".docx");
                PriorityFiles.Add(".pdf");
                ExtensionsToCrypt.Add(".txt");
                ExtensionsToCrypt.Add(".docx");
                ExtensionsToCrypt.Add(".pdf");
            }
        }

        // Saves the current settings to the JSON file
        private void SaveSettings()
        {
            try
            {
                // Create the settings folder if it does not exist
                string settingsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\\..\\..\\", "Settings");
                if (!Directory.Exists(settingsFolder))
                {
                    Directory.CreateDirectory(settingsFolder);
                }

                string settingsFilePath = Path.Combine(settingsFolder, "settings.json");

                // Create the settings object
                var settings = new EasySave.Models.AppSettings
                {
                    Language = SelectedLanguage,
                    LogFormat = SelectedLogFormat,
                    PriorityExtensions = PriorityFiles.ToList(),
                    ExtensionsToCrypt = ExtensionsToCrypt.ToList(),
                    BusinessSoftware = new EasySave.Models.BusinessSoftware
                    {
                        Name = CurrentBusinessSoftware,
                        FullPath = SelectedBusinessSoftware?.FullPath ?? ""
                    }
                };

                // Serialize the settings object to JSON
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(settingsFilePath, json);

                _loggerService?.Log("Settings saved successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement des paramètres : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggerService?.LogError($"Error saving settings: {ex.Message}");
            }
        }
    }

    // Class representing a process for business software selection
    public class ProcessInfo
    {
        public string Name { get; set; }
        public string FullPath { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    // Generic RelayCommand implementation for ICommand
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
