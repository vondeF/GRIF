using System;
using System.Windows.Input;

namespace GRIF.ViewModels
{
    public class RelayCommand : ICommand
    {
        // Делегат, который будет вызван при выполнении команды
        private readonly Action<object?> _execute;

        // Делегат, который определяет, можно ли выполнить команду
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Метод, который вызывается WPF, чтобы узнать, можно ли выполнить команду в данный момент
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        // Метод, который вызывается при выполнении команды
        public void Execute(object? parameter) => _execute(parameter);

        // Событие, которое позволяет WPF-элементам узнавать, что состояние команды изменилось
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; } // При подписке на событие — добавляем стандартный обработчик WPF
            remove { CommandManager.RequerySuggested -= value; } // При отписке — убираем его
        }
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private readonly Predicate<object?>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                _isExecuting = true;
                OnCanExecuteChanged();

                try
                {
                    await _execute(parameter);
                }
                finally
                {
                    _isExecuting = false;
                    OnCanExecuteChanged();
                }
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        protected virtual void OnCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested(); // Обновляем состояние команды
        }
    }
}
