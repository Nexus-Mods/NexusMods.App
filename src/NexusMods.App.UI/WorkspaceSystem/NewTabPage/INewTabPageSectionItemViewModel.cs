using Avalonia.Media;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageSectionItemViewModel : IViewModelInterface
{
    public string Name { get; }

    public IImage? Icon { get; }
}
