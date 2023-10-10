using Avalonia.Controls;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

public interface IModContentViewModel : IViewModel
{
    public HierarchicalTreeDataGridSource<IModContentNode> Tree { get; }
}
