using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageSectionViewModel : AViewModel<INewTabPageSectionViewModel>, INewTabPageSectionViewModel
{
    public string SectionName { get; }

    private readonly SourceList<INewTabPageSectionItemViewModel> _itemSource = new();
    private readonly ReadOnlyObservableCollection<INewTabPageSectionItemViewModel> _items;
    public ReadOnlyObservableCollection<INewTabPageSectionItemViewModel> Items => _items;

    public ReactiveCommand<PageData, PageData> SelectItemCommand { get; } = ReactiveCommand.Create<PageData, PageData>(pageData => pageData);

    public NewTabPageSectionViewModel(string sectionName, PageDiscoveryDetails[] discoveryDetails)
    {
        SectionName = sectionName;

        _itemSource.Edit(list =>
        {
            var toAdd = discoveryDetails
                .Select(x => (INewTabPageSectionItemViewModel)new NewTabPageSectionItemViewModel(x));
            list.AddRange(toAdd);
        });

        _itemSource
            .Connect()
            .Bind(out _items)
            .Subscribe();

        this.WhenActivated(disposables =>
        {
            _itemSource
                .Connect()
                .MergeMany(item => item.SelectItemCommand)
                .Do(_ => Console.WriteLine("section view model"))
                .InvokeCommand(SelectItemCommand)
                .DisposeWith(disposables);
        });
    }
}
