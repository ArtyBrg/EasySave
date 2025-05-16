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

namespace EasySave.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {

        private readonly LoggerService _loggerService;

        private StringBuilder _logBuilder = new StringBuilder();
        private string _logContent = string.Empty;

        private string _currentLanguage = "FR"; // Par défaut en français
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged();
                }
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
            CurrentLanguage = language;
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
            const string calculatorName = "calc.exe";

            // Recherche de la calculatrice dans les processus existants
            var calculator = AvailableProcesses.FirstOrDefault(p => p.Name.Equals(calculatorName, StringComparison.OrdinalIgnoreCase));

            if (calculator == null)
            {
                // Ajout de la calculatrice si elle n'est pas trouvée
                calculator = new ProcessInfo
                {
                    Name = calculatorName,
                    FullPath = "calc.exe" // Le chemin complet n'est pas nécessaire pour la calculatrice
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
            _loggerService?.Log($"- Language: {CurrentLanguage}");
            _loggerService?.Log($"- Log format: {SelectedLogFormat}");
            _loggerService?.Log($"- Extensions to encrypt: {string.Join(", ", ExtensionsToCrypt)}");
            _loggerService?.Log($"- Business software: {CurrentBusinessSoftware}");
            _loggerService?.Log("Settings applied successfully.");

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
                        if (!string.IsNullOrEmpty(process.MainWindowTitle))
                        {
                            AvailableProcesses.Add(new ProcessInfo
                            {
                                Name = process.ProcessName + ".exe",
                                FullPath = GetProcessFilePath(process)
                            });
                        }
                    }
                    catch
                    {
                        // Ignore les processus auxquels on n'a pas accès
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
                        CurrentLanguage = settings.Language;
                        _loggerService?.Log($"Language loaded from settings: {CurrentLanguage}");
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
                    Language = CurrentLanguage,
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