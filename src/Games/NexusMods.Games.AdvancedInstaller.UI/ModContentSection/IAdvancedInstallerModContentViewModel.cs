using System.Collections.ObjectModel;
using Avalonia.Controls;
using NexusMods.App.UI;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IAdvancedInstallerModContentViewModel : IViewModel
{
    public HierarchicalTreeDataGridSource<TreeDataGridFileNode> Tree { get;  }
}

public class TreeDataGridFileNode : ReactiveObject
{
    [Reactive] public bool IsRoot { get; set; } = false;

    [Reactive] public string FileName { get; set; } = string.Empty;

    [Reactive] public bool IsDirectory { get; set; } = false;

    [Reactive] public ObservableCollection<TreeDataGridFileNode> Children { get; set; } = new();
}
