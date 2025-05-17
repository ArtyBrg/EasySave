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

                            // Rendre l'application inactive pendant ce temps
                            Application.Current.MainWindow.IsEnabled = false;

                            _businessPopupWindow.Show();
                        }
                    }
                    else
                    {
                        if (_businessPopupWindow != null)
                        {
                            _businessPopupWindow.Close();
                            _businessPopupWindow = null;

                            Application.Current.MainWindow.IsEnabled = true;

                            // Forcer la fenêtre principale à revenir au premier plan
                            Application.Current.MainWindow.Activate();
                            Application.Current.MainWindow.Topmost = true;    // Place au-dessus
                            Application.Current.MainWindow.Topmost = false;   // Réinitialise pour permettre d'autres fenêtrages plus tard
                            Application.Current.MainWindow.Focus();
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
        // Format de log
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

        // Extensions à crypter
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

        // Logiciel métier
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



        // Commandes
        public ICommand SetLanguageCommand { get; private set; }
        public ICommand AddExtensionCommand { get; private set; }
        public ICommand RemoveExtensionCommand { get; private set; }
        public ICommand BrowseBusinessSoftwareCommand { get; private set; }
        public ICommand UseCalculatorCommand { get; private set; }
        public ICommand ApplySettingsCommand { get; private set; }


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

            LoadRunningProcesses();
            LoadSettings();

            _businessSoftwareCheckTimer = new System.Timers.Timer(2000); // 5000 ms = 5 secondes
            _businessSoftwareCheckTimer.Elapsed += CheckBusinessSoftwareRunning;
            _businessSoftwareCheckTimer.Start();

        }

        private void OnLogMessageAdded(object sender, string message)
        {
            // Ajouter le message à notre builder
            _logBuilder.AppendLine(message);

            // Mettre à jour la propriété LogContent avec le contenu complet
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
            // Logique pour changer la langue de l'application
            _loggerService?.Log($"Language setting changed to {language}");
        }

        private void AddExtension(object parameter)
        {
            if (string.IsNullOrWhiteSpace(FileExtensionToCrypt))
                return;

            // Formatage de l'extension (ajout du point si nécessaire)
            string extension = FileExtensionToCrypt.Trim();
            if (!extension.StartsWith("."))
                extension = "." + extension;

            // Vérification si l'extension existe déjà
            if (!ExtensionsToCrypt.Contains(extension))
            {
                ExtensionsToCrypt.Add(extension);
                _loggerService?.Log($"Extension added to encryption list: {extension}");
            }

            // Réinitialisation du champ
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

                // Création d'un nouveau ProcessInfo pour le logiciel sélectionné
                var processInfo = new ProcessInfo
                {
                    Name = fileName,
                    FullPath = openFileDialog.FileName
                };

                // Ajout à la liste si non présent
                if (!AvailableProcesses.Any(p => p.FullPath == processInfo.FullPath))
                {
                    AvailableProcesses.Add(processInfo);
                }

                // Sélection du processus
                SelectedBusinessSoftware = processInfo;
                _loggerService?.Log($"Business software selected: {fileName}");
            }
        }

        private void UseCalculator(object parameter)
        {
            const string calculatorName = "CalculatorApp";

            // Recherche de la calculatrice dans les processus existants
            var calculator = AvailableProcesses.FirstOrDefault(p => p.Name.Equals(calculatorName, StringComparison.OrdinalIgnoreCase));

            if (calculator == null)
            {
                // Ajout de la calculatrice si elle n'est pas trouvée
                calculator = new ProcessInfo
                {
                    Name = calculatorName,
                    FullPath = "CalculatorApp" // Le chemin complet n'est pas nécessaire pour la calculatrice
                };
                AvailableProcesses.Add(calculator);
            }

            // Sélection de la calculatrice
            SelectedBusinessSoftware = calculator;
            _loggerService?.Log("Calculator set as business software");
        }

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

                        // Filtrer les vrais logiciels (optionnel)
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
                        // Accès refusé à certains processus
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des processus : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggerService?.LogError($"Error loading processes: {ex.Message}");
            }
        }


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

        private void LoadSettings()
        {
            try
            {
                var settings = SettingsService.Load();

                // Appliquer les paramètres chargés
                if (settings != null)
                    {
                        SelectedLanguage = settings.Language;
                        _loggerService?.Log($"Language loaded from settings: {SelectedLanguage}");
                        SelectedLogFormat = settings.LogFormat;
                        _loggerService?.Log($"Log format loaded from settings: {SelectedLogFormat}");

                        // Charger les extensions
                        ExtensionsToCrypt.Clear();
                        int i = 0;
                        foreach (var extension in settings.ExtensionsToCrypt)
                        {
                            i += 1;
                            ExtensionsToCrypt.Add(extension);
                            _loggerService?.Log($"Extension to crypt number {i} loaded from settings: {extension}");
                        }

                        // Charger le logiciel métier
                        if (!string.IsNullOrEmpty(settings.BusinessSoftware.Name) && settings.BusinessSoftware.Name != "Aucun")
                        {
                            CurrentBusinessSoftware = settings.BusinessSoftware.Name;

                            // Ajout du logiciel métier à la liste si non présent
                            var businessSoftware = new ProcessInfo
                            {
                                Name = settings.BusinessSoftware.Name,
                                FullPath = settings.BusinessSoftware.FullPath
                            };

                            if (!AvailableProcesses.Any(p => p.FullPath == businessSoftware.FullPath))
                            {
                                AvailableProcesses.Add(businessSoftware);
                            }

                            // Sélection du logiciel métier
                            SelectedBusinessSoftware = businessSoftware;
                            _loggerService?.Log($"Business software loaded from settings: {SelectedBusinessSoftware}");
                        }

                        // Appliquer le format de log
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
                    // Fichier non trouvé, charger des valeurs par défaut
                    ExtensionsToCrypt.Add(".txt");
                    ExtensionsToCrypt.Add(".docx");
                    ExtensionsToCrypt.Add(".pdf");

                    // Créer et sauvegarder un fichier de paramètres par défaut
                    SaveSettings();

                    _loggerService?.Log("Created default settings file");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des paramètres : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggerService?.LogError($"Error loading settings: {ex.Message}");

                // Charger des valeurs par défaut en cas d'erreur
                ExtensionsToCrypt.Add(".txt");
                ExtensionsToCrypt.Add(".docx");
                ExtensionsToCrypt.Add(".pdf");
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Création du dossier Settings s'il n'existe pas

                string settingsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\\..\\..\\", "Settings");
                if (!Directory.Exists(settingsFolder))
                {
                    Directory.CreateDirectory(settingsFolder);
                }

                string settingsFilePath = Path.Combine(settingsFolder, "settings.json");

                // Créer l'objet de paramètres
                var settings = new EasySave.Models.AppSettings
                {
                    Language = SelectedLanguage,
                    LogFormat = SelectedLogFormat,
                    ExtensionsToCrypt = ExtensionsToCrypt.ToList(),
                    BusinessSoftware = new EasySave.Models.BusinessSoftware
                    {
                        Name = CurrentBusinessSoftware,
                        FullPath = SelectedBusinessSoftware?.FullPath ?? ""
                    }
                };

                // Sérialiser et enregistrer
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