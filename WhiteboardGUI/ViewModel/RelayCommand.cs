//using System;
//using System.Diagnostics;
//using System.Windows.Input;

//namespace WhiteboardGUI.ViewModel
//{
//    /// <summary>
//    /// A command that relays its functionality to other objects.
//    /// </summary>
//    public class RelayCommand : ICommand
//    {
//        private readonly Action _execute;
//        private readonly Func<bool> _canExecute;

//        /// <summary>
//        /// Initializes a new instance of the RelayCommand class.
//        /// </summary>
//        /// <param name="execute">The execution logic.</param>
//        public RelayCommand(Action execute)
//            : this(execute, null)
//        {
//        }

//        /// <summary>
//        /// Initializes a new instance of the RelayCommand class.
//        /// </summary>
//        /// <param name="execute">The execution logic.</param>
//        /// <param name="canExecute">The execution status logic.</param>
//        public RelayCommand(Action execute, Func<bool> canExecute)
//        {
//            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
//            _canExecute = canExecute;
//        }

//        [DebuggerStepThrough]
//        public bool CanExecute(object parameter) =>
//            _canExecute == null || _canExecute();

//        /// <summary>
//        /// Occurs when changes affect whether or not the command should execute.
//        /// </summary>
//        public event EventHandler CanExecuteChanged
//        {
//            add { CommandManager.RequerySuggested += value; }
//            remove { CommandManager.RequerySuggested -= value; }
//        }

//        public void Execute(object parameter) => _execute();
//    }

//    /// <summary>
//    /// A generic command that relays its functionality to other objects.
//    /// </summary>
//    /// <typeparam name="T">The type of the command parameter.</typeparam>
//    public class RelayCommand<T> : ICommand
//    {
//        private readonly Action<T> _execute;
//        private readonly Predicate<T> _canExecute;

//        /// <summary>
//        /// Initializes a new instance of the RelayCommand class.
//        /// </summary>
//        /// <param name="execute">The execution logic.</param>
//        public RelayCommand(Action<T> execute)
//            : this(execute, null)
//        {
//        }

//        /// <summary>
//        /// Initializes a new instance of the RelayCommand class with canExecute logic.
//        /// </summary>
//        /// <param name="execute">The execution logic.</param>
//        /// <param name="canExecute">The execution status logic.</param>
//        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
//        {
//            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
//            _canExecute = canExecute;
//        }

//        [DebuggerStepThrough]
//        public bool CanExecute(object parameter)
//        {
//            if (parameter is T typedParameter)
//            {
//                return _canExecute == null || _canExecute(typedParameter);
//            }
//            return false;
//        }

//        /// <summary>
//        /// Occurs when changes affect whether or not the command should execute.
//        /// </summary>
//        public event EventHandler CanExecuteChanged
//        {
//            add { CommandManager.RequerySuggested += value; }
//            remove { CommandManager.RequerySuggested -= value; }
//        }

//        public void Execute(object parameter)
//        {
//            if (parameter is T typedParameter)
//            {
//                _execute(typedParameter);
//            }
//            else
//            {
//                throw new InvalidCastException($"Invalid parameter type. Expected {typeof(T)}, but got {parameter.GetType()}.");
//            }
//        }
//    }
//}
using System;
using System.Diagnostics;
using System.Windows.Input;

namespace WhiteboardGUI.ViewModel
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter) =>
            _canExecute == null || _canExecute();

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter) =>
            _canExecute == null || _canExecute((T)parameter);

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter) => _execute((T)parameter);
    }
}

