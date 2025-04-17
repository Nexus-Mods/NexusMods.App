using Avalonia.Controls;

namespace NexusMods.App.UI.MessageBox;

public interface IMessageBox<T>
{
    /// <summary>
    ///  Show messagebox as window
    /// </summary>
    /// <returns></returns>
    Task<T> ShowWindowAsync();

    /// <summary>
    ///  Show messagebox as window with owner
    /// </summary>
    /// <param name="owner">Window owner </param>
    /// <returns></returns>
    Task<T> ShowWindowDialogAsync(Window owner);

    
}
