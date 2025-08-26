using Avalonia.Threading;

namespace NexusMods.UI.Sdk;

public static class DispatcherHelper
{
    
    /// <summary>
    /// Ensures the provided action is executed on the UI thread.
    /// </summary>
    public static Task EnsureOnUIThreadAsync(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }
        
        return Dispatcher.UIThread.InvokeAsync(() =>
            {
                action();
                return Task.CompletedTask;
            }
        );
    }
    
    /// <summary>
    /// Ensures the provided function is executed on the UI thread.
    /// </summary>
    public static Task<TResult> EnsureOnUIThreadAsync<TResult>(Func<TResult> func)
    {
        if (Dispatcher.UIThread.CheckAccess())
            return Task.FromResult(func());
        
        return Dispatcher.UIThread.InvokeAsync(() => Task.FromResult(func()));
    }
    
    
    
    /// <summary>
    /// Ensures the provided task is executed on the UI thread.
    /// </summary>
    public static Task EnsureOnUIThreadAsync(Func<Task> task)
    {
        if (Dispatcher.UIThread.CheckAccess())
            return task();
        
        return Dispatcher.UIThread.InvokeAsync(task);
    }
    
    /// <summary>
    /// Ensures the provided async function is executed on the UI thread.
    /// </summary>
    public static Task<TResult> EnsureOnUIThreadAsync<TResult>(Func<Task<TResult>> func)
    {
        if (Dispatcher.UIThread.CheckAccess())
            return func();
        
        return Dispatcher.UIThread.InvokeAsync(func);
    }
    
    public static void EnsureOnUIThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }
        
        Dispatcher.UIThread.Invoke(action);
    }
}
