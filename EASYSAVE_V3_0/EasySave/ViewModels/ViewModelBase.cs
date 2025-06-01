using EasySave.Services;
using EasySave_WPF;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasySave.ViewModels
{
    // ViewModelBase is a base class for all view models in the application.
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // SetProperty is a helper method to set a property value and raise the PropertyChanged event if the value has changed.
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // The AppViewModel property provides access to the main application view model.
        public TranslationProvider Translations => App.AppViewModel.Translations;
        public void ChangeLanguage(string langCode)
        {
            App.AppViewModel.ChangeLanguages(langCode);
        }
    }
}
