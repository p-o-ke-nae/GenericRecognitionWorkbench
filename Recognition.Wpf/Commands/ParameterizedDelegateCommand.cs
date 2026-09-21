using System.Windows.Input;

namespace Recognition.Wpf;

public sealed class ParameterizedDelegateCommand<T>(Action<T?> execute, Predicate<T?>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return canExecute?.Invoke(Convert(parameter)) ?? true;
    }

    public void Execute(object? parameter)
    {
        execute(Convert(parameter));
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private static T? Convert(object? parameter)
    {
        if (parameter is null)
        {
            return default;
        }

        return parameter is T typed
            ? typed
            : default;
    }
}
