using System.Globalization;
using System.Resources;
using System.Threading;
using EasySave;

public class LanguageManager
{
    private static ResourceManager _resourceManager = new("EasySave.Resources", typeof(LanguageManager).Assembly);

    public void SetLanguage(string languageCode)
    {
        var culture = languageCode.ToUpper() switch
        {
            "EN" => new CultureInfo("en"),
            "FR" => new CultureInfo("fr"),
            _ => throw new ArgumentException("Invalid language. Please enter EN or FR.")
        };

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    public string GetString(string key)
    {
        return _resourceManager.GetString(key) ?? $"[{key}]";
    }
}