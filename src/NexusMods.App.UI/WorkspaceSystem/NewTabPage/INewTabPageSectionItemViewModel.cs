using NexusMods.App.UI.Controls.Navigation;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageSectionItemViewModel : IViewModelInterface
{
    public string SectionName { get; }

    public string Name { get; }

    public IconValue Icon { get; }

    public ReactiveCommand<NavigationInformation, ValueTuple<PageData, NavigationInformation>> SelectItemCommand { get; }
}
