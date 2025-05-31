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

        private Window _businessPopupWindow;

        private readonly LoggerService _loggerService;

        private System.Timers.Timer _businessSoftwareCheckTimer;
        private bool _popupAlreadyShown = false;


        private StringBuilder _logBuilder = new StringBuilder();
        private string _logContent = string.Empty;

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

        // Check if the business software is running
        private void CheckBusinessSoftwareRunning(object sender, ElapsedEventArgs e)
        {
            try
            {
                var settings = SettingsService.Instance.Load();

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

                           
                            Application.Current.MainWindow.IsEnabled = false;
                            _businessPopupWindow.Show();

                            _loggerService?.Log("Job software detected. Automatic pause of jobs...");
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

                            // Highlights the main window
                            Application.Current.MainWindow.Activate();
                            Application.Current.MainWindow.Topmost = true;
                            Application.Current.MainWindow.Topmost = false;
                            Application.Current.MainWindow.Focus();

                            // AUTOMATIC JOB RESUMPTION
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
        
        public ObservableCollection<string> LogFormats { get; } = new ObservableCollection<string> { "JSON", "XML" };
        // Logs formats
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

        // Liste des fichiers à prioriser
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

        // Fichier à ajouter à la liste
        private string _fileToPrioritize= "";
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
        
        // Extensions to encrypt
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

        // Job logiciel
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



        // Commands
        public ICommand SetLanguageCommand { get; private set; }
        public ICommand AddExtensionCommand { get; private set; }
        public ICommand RemoveExtensionCommand { get; private set; }
        public ICommand BrowseBusinessSoftwareCommand { get; private set; }
        public ICommand UseCalculatorCommand { get; private set; }
        public ICommand ApplySettingsCommand { get; private set; }
        public ICommand AddPriorityFileCommand { get; private set; }
        public ICommand RemovePriorityFileCommand { get; private set; }
        
        // Constructor
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

        private void OnLogMessageAdded(object sender, string message)
        {
            // Add the new log message to the StringBuilder
            _logBuilder.AppendLine(message);

            // Update the LogContent property to reflect the new log message
            LogContent = _logBuilder.ToString();
        }

        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        private void SetLanguage(string language)
        {
            SelectedLanguage = language;
            // Logic to change the language of the application
            _loggerService?.Log($"Language setting changed to {language}");
        }

        private void AddPriorityFile(object parameter)
        {
            if (string.IsNullOrWhiteSpace(FileToPrioritize))
                return;

            // Format of the priority
            string priority = FileToPrioritize.Trim();
            if (!priority.StartsWith("."))
                priority = "." + priority;

            // Verification if the priority is already in the list
            if (!PriorityFiles.Contains(priority))
            {
                PriorityFiles.Add(priority);
                _loggerService?.Log($"priority added to encryption list: {priority}");
            }

            // Reinitialization of the input field
            FileToPrioritize = "";
        }

        private void RemovePriorityFile(string priority)
        {
            if (priority != null)
            {
                PriorityFiles.Remove(priority);
                _loggerService?.Log($"Priority removed from priority list: {priority}");
            }
        }

        private void AddExtension(object parameter)
        {
            if (string.IsNullOrWhiteSpace(FileExtensionToCrypt))
                return;

            // Format of the extension
            string extension = FileExtensionToCrypt.Trim();
            if (!extension.StartsWith("."))
                extension = "." + extension;

            // Verification if the extension is already in the list
            if (!ExtensionsToCrypt.Contains(extension))
            {
                ExtensionsToCrypt.Add(extension);
                _loggerService?.Log($"Extension added to encryption list: {extension}");
            }

            // Reinitialization of the input field
            FileExtensionToCrypt = "";
        }

        private void RemoveExtension(string extension)
        {
            if (extension != null)
            {
                ExtensionsToCrypt.Remove(extension);
                _loggerService?.Log($"Extension removed from encryption list: {extension}");
            }
        }

        // Open a file dialog to select the business software
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

                // Creation of the ProcessInfo object
                var processInfo = new ProcessInfo
                {
                    Name = fileName,
                    FullPath = openFileDialog.FileName
                };

                // Addition of the process to the list if not already present
                if (!AvailableProcesses.Any(p => p.FullPath == processInfo.FullPath))
                {
                    AvailableProcesses.Add(processInfo);
                }

                // Selection of the process
                SelectedBusinessSoftware = processInfo;
                _loggerService?.Log($"Business software selected: {fileName}");
            }
        }

        // Use the calculator as business software
        private void UseCalculator(object parameter)
        {
            const string calculatorName = "CalculatorApp";

            // Research of the calculator in the list of processes
            var calculator = AvailableProcesses.FirstOrDefault(p => p.Name.Equals(calculatorName, StringComparison.OrdinalIgnoreCase));

            if (calculator == null)
            {
                // Addition of the calculator to the list if not present
                calculator = new ProcessInfo
                {
                    Name = calculatorName,
                    FullPath = "CalculatorApp" // Path not necessary for the calculator
                };
                AvailableProcesses.Add(calculator);
            }

            // Selection of the calculator
            SelectedBusinessSoftware = calculator;
            _loggerService?.Log("Calculator set as business software");
        }

        // Apply the settings
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

        // Load the running processes
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

                        // Filter the processes to only include those in "C:\Program Files" or "C:\Program Files (x86)"
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
                        // Access denied to the process
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des processus : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggerService?.LogError($"Error loading processes: {ex.Message}");
            }
        }


        // Get the file path of the process
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

        // Load the settings from the JSON file
        private void LoadSettings()
        {
            try
            {
                var settings = SettingsService.Instance.Load();

                // Apply the settings if the file exists
                if (settings != null)
                {
                    SelectedLanguage = settings.Language;
                    _loggerService?.Log($"Language loaded from settings: {SelectedLanguage}");
                    SelectedLogFormat = settings.LogFormat;
                    _loggerService?.Log($"Log format loaded from settings: {SelectedLogFormat}");

                    // Charge of the extensions to crypt
                    ExtensionsToCrypt.Clear();
                    int i = 0;
                    foreach (var extension in settings.ExtensionsToCrypt)
                    {
                        i += 1;
                        ExtensionsToCrypt.Add(extension);
                        _loggerService?.Log($"Extension to crypt number {i} loaded from settings: {extension}");
                    }

                    PriorityFiles.Clear();
                    // Charge of the priority files
                    foreach (var priority in settings.PriorityExtensions)
                    {
                        PriorityFiles.Add(priority);
                        _loggerService?.Log($"Priority file loaded from settings: {priority}");
                    }

                    // Charge of the business software
                    if (!string.IsNullOrEmpty(settings.BusinessSoftware.Name) && settings.BusinessSoftware.Name != "Aucun")
                    {
                        CurrentBusinessSoftware = settings.BusinessSoftware.Name;

                        // Addition of the business software to the list if not already present
                        var businessSoftware = new ProcessInfo
                        {
                            Name = settings.BusinessSoftware.Name,
                            FullPath = settings.BusinessSoftware.FullPath
                        };

                        if (!AvailableProcesses.Any(p => p.FullPath == businessSoftware.FullPath))
                        {
                            AvailableProcesses.Add(businessSoftware);
                        }

                        // Selection of the business software
                        SelectedBusinessSoftware = businessSoftware;
                        _loggerService?.Log($"Business software loaded from settings: {SelectedBusinessSoftware}");
                    }

                    // Apply the log format
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
                    // File not found, create a default settings file
                    ExtensionsToCrypt.Add(".txt");
                    ExtensionsToCrypt.Add(".docx");
                    ExtensionsToCrypt.Add(".pdf");

                    PriorityFiles.Add(".txt");
                    PriorityFiles.Add(".docx");
                    PriorityFiles.Add(".pdf");

                    // Create and save the default settings
                    SaveSettings();

                    _loggerService?.Log("Created default settings file");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des paramètres : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggerService?.LogError($"Error loading settings: {ex.Message}");

                // Charge of the default extensions
                PriorityFiles.Add(".txt");
                PriorityFiles.Add(".docx");
                PriorityFiles.Add(".pdf");
                ExtensionsToCrypt.Add(".txt");
                ExtensionsToCrypt.Add(".docx");
                ExtensionsToCrypt.Add(".pdf");
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Creation of the settings folder if it does not exist

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

    // Class representing a process
    public class ProcessInfo
    {
        public string Name { get; set; }
        public string FullPath { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

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