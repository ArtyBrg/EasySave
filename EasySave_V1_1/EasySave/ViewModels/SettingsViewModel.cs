// ViewModels/SettingsViewModel.cs
using EasySave.Models;
using LoggerLib;

// Service for managing application settings, including log format and language
public class SettingsViewModel
{
    // Service for loading and saving settings
    public AppSettings Settings { get; private set; }

    // Service for logging actions, errors, and file transfers
    public SettingsViewModel()
    {
        Settings = SettingsService.Load();
    }

    // Method to change the log format and save the settings
    public void ChangeLogFormat(LogFormat newFormat)
    {
        Settings.LogFormat = newFormat;
        SettingsService.Save(Settings);
    }

    // Method to change the language and save the settings
    public LogFormat GetCurrentFormat() => Settings.LogFormat;
}
