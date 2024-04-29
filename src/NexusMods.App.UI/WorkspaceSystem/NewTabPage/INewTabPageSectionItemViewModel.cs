using Avalonia.Media;
using NexusMods.App.UI.Controls.Navigation;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageSectionItemViewModel : IViewModelInterface
{
    public string SectionName { get; }

    public string Name { get; }

    public IImage? Icon { get; }

    public ReactiveCommand<NavigationInformation, ValueTuple<PageData, NavigationInformation>> SelectItemCommand { get; }
}
