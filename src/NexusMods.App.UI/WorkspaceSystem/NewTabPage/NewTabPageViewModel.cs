using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData;
using NexusMods.App.UI.Windows;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageViewModel : APageViewModel<INewTabPageViewModel>, INewTabPageViewModel
{
    private readonly SourceList<INewTabPageSectionItemViewModel> _itemSource = new();

    private readonly ReadOnlyObservableCollection<INewTabPageSectionViewModel> _sections;
    public ReadOnlyObservableCollection<INewTabPageSectionViewModel> Sections => _sections;

    public NewTabPageViewModel(IWindowManager windowManager, PageDiscoveryDetails[] discoveryDetails) : base(windowManager)
    {
        _itemSource.Edit(list =>
        {
            var toAdd = discoveryDetails
                .Select(details => (INewTabPageSectionItemViewModel)new NewTabPageSectionItemViewModel(details));

            list.AddRange(toAdd);
        });

        _itemSource
            .Connect()
            .GroupOn(item => item.SectionName)
            .Transform(x => (INewTabPageSectionViewModel)new NewTabPageSectionViewModel(x.GroupKey, x.List))
            .Bind(out _sections)
            .Subscribe();

        this.WhenActivated(disposables =>
        {
            _itemSource
                .Connect()
                .MergeMany(item => item.SelectItemCommand)
                .SubscribeWithErrorLogging(pageData =>
                {
                    GetWorkspaceController().OpenPage(WorkspaceId, pageData, new OpenPageBehavior(new OpenPageBehavior.ReplaceTab(PanelId, TabId)));
                })
                .DisposeWith(disposables);
        });
    }
}
