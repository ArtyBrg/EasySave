// ViewModels/SettingsViewModel.cs
using EasySave.Models;
using LoggerLib;

public class SettingsViewModel
{
    public AppSettings Settings { get; private set; }

    public SettingsViewModel()
    {
        Settings = SettingsService.Load();
    }

    public void ChangeLogFormat(LogFormat newFormat)
    {
        Settings.LogFormat = newFormat;
        SettingsService.Save(Settings);
    }

    public LogFormat GetCurrentFormat() => Settings.LogFormat;
}
