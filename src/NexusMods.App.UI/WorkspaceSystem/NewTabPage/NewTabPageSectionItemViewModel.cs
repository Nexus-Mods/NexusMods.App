using System.Reactive;
using Avalonia.Media;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageSectionItemViewModel : AViewModel<INewTabPageSectionItemViewModel>, INewTabPageSectionItemViewModel
{
    public string Name { get; }

    public IImage? Icon { get; }

    public ReactiveCommand<Unit, PageData> SelectItemCommand { get; }

    public NewTabPageSectionItemViewModel(PageDiscoveryDetails details)
    {
        Name = details.ItemName;
        Icon = null;

        SelectItemCommand = ReactiveCommand.Create(() => details.PageData);
    }
}
