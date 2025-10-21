using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.UI.Sdk;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageSectionViewModel : AViewModel<INewTabPageSectionViewModel>, INewTabPageSectionViewModel
{
    public string SectionName { get; }

    private readonly ReadOnlyObservableCollection<INewTabPageSectionItemViewModel> _items;
    public ReadOnlyObservableCollection<INewTabPageSectionItemViewModel> Items => _items;

    public NewTabPageSectionViewModel(string sectionName, IObservableList<INewTabPageSectionItemViewModel> items)
    {
        SectionName = sectionName;

        items
            .Connect()
            .Bind(out _items)
            .Subscribe();
    }
}
