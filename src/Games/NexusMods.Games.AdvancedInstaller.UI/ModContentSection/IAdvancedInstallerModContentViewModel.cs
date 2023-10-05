using Avalonia.Controls;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IAdvancedInstallerModContentViewModel : IViewModel
{
    public HierarchicalTreeDataGridSource<ITreeDataGridSourceFileNode> Tree { get; }
}
