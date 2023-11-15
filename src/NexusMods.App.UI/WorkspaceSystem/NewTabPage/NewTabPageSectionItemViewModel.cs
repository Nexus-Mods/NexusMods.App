using System.Reactive;
using Avalonia.Media;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageSectionItemViewModel : AViewModel<INewTabPageSectionItemViewModel>, INewTabPageSectionItemViewModel
{
    public string SectionName { get; }

    public string Name { get; }

    public IImage? Icon { get; }

    public ReactiveCommand<Unit, PageData> SelectItemCommand { get; }

    public NewTabPageSectionItemViewModel(PageDiscoveryDetails details)
    {
        SectionName = details.SectionName;
        Name = details.ItemName;
        Icon = null;

        SelectItemCommand = ReactiveCommand.Create(() => details.PageData);
    }
}
