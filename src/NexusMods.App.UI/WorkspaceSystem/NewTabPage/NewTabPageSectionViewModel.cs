using System.Collections.ObjectModel;
using DynamicData;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageSectionViewModel : AViewModel<INewTabPageSectionViewModel>, INewTabPageSectionViewModel
{
    public string SectionName { get; }

    private readonly SourceList<INewTabPageSectionItemViewModel> _itemSource = new();
    private readonly ReadOnlyObservableCollection<INewTabPageSectionItemViewModel> _items;
    public ReadOnlyObservableCollection<INewTabPageSectionItemViewModel> Items => _items;

    public NewTabPageSectionViewModel(string sectionName, INewTabPageSectionItemViewModel[] items)
    {
        SectionName = sectionName;

        _itemSource.Edit(list => list.AddRange(items));
        _itemSource
            .Connect()
            .Bind(out _items)
            .Subscribe();
    }
}
