using System.Collections.ObjectModel;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface INewTabPageSectionViewModel : IViewModelInterface
{
    public string SectionName { get; }

    public ReadOnlyObservableCollection<INewTabPageSectionItemViewModel> Items { get; }

    public ReactiveCommand<PageData, PageData> SelectItemCommand { get; }
}
