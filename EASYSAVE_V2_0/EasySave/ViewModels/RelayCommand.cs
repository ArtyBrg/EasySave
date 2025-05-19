using System;
using System.Windows.Input;
using EasySave_WPF;

namespace EasySave.ViewModels
{
    //RelayCommand is a class that implements the ICommand interface.
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // The CanExecute method determines whether the command can execute in its current state.
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        // The Execute method defines the action to be taken when the command is executed.
        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        // This method raises the CanExecuteChanged event to indicate that the command's ability to execute may have changed.
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
