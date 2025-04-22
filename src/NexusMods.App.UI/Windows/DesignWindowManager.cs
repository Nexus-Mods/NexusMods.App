using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using NexusMods.App.UI.MessageBox;
using NexusMods.App.UI.MessageBox.Enums;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Windows;

public class DesignWindowManager : IWindowManager
{
    public ReadOnlyObservableCollection<WindowId> AllWindowIds { get; } = ReadOnlyObservableCollection<WindowId>.Empty;
    [Reactive] public IWorkspaceWindow ActiveWindow { get; set; } = null!;

    public bool TryGetWindow(WindowId windowId, [NotNullWhen(true)] out IWorkspaceWindow? window)
    {
        window = null;
        return false;
    }

    public void RegisterWindow(IWorkspaceWindow window) { }

    public void UnregisterWindow(IWorkspaceWindow window) { }

    public void SaveWindowState(IWorkspaceWindow window) { }
    public bool RestoreWindowState(IWorkspaceWindow window) => false;
    public Task<ButtonDefinitionId> ShowMessageBox(IMessageBox<ButtonDefinitionId> messageBox, MessageBoxWindowType windowType)
    {
        throw new NotImplementedException();
    }
}
