namespace Countdown.ViewModels;

/// <summary>
/// An ICommand implementation that calls an asynchronous task Execute() method in 
/// a fire and forget fashion i.e. it's an async void explicit Execute() method call.
/// It automatically updates the CanExecute() method to return false if the task hasn't
/// completed. In addition it provides an implicit Execute() method that returns the
/// Task object which can be awaited on. This is to allow for unit testing when the 
/// results of Execute() method need to be awaited on for the results to be valid.
/// This class is not thread safe. In a multi-threaded environment there is no 
/// correlation between the CanExecute() method and the Execute() method. 
/// </summary>
internal sealed class RelayTaskCommand : ICommand
{
    private readonly Func<object?, Task> execute;
    private readonly Func<object?, bool, bool> canExecute;
    public event EventHandler? CanExecuteChanged;

    private bool isExecuting = false;


    public RelayTaskCommand(Func<object?, Task> execute, Func<object?, bool, bool> canExecute)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    /// <summary>
    ///  Allows access to the delegates task so that unit tests can await 
    ///  on its completion, after which any results will be valid
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public Task Execute(object? parameter)
    {
        return execute(parameter);
    }


    public bool CanExecute(object? parameter)
    {
        return canExecute(parameter, isExecuting);
    }


    async void ICommand.Execute(object? parameter)
    {
        isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await Execute(parameter);
        }
        finally
        {
            isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
