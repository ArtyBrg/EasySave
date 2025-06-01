using System;
using System.Windows.Input;

namespace EasySaveClient.ViewModels
{
    // RelayCommand implements ICommand for binding UI actions to view model logic
    public class RelayCommand : ICommand
    {
        // Action to execute when the command is invoked
        private readonly Action<object> _execute;
        // Predicate to determine if the command can execute
        private readonly Predicate<object> _canExecute;

        // Constructor accepting the execute action and optional canExecute predicate
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Determines whether the command can execute with the given parameter
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        // Executes the command action
        public void Execute(object parameter) => _execute(parameter);

        // Event to re-evaluate command executability (WPF infrastructure)
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}

