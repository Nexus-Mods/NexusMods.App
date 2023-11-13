using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageViewModel : APageViewModel<INewTabPageViewModel>, INewTabPageViewModel
{
    private readonly SourceList<INewTabPageSectionViewModel> _sectionSource = new();
    private readonly ReadOnlyObservableCollection<INewTabPageSectionViewModel> _sections;
    public ReadOnlyObservableCollection<INewTabPageSectionViewModel> Sections => _sections;

    public NewTabPageViewModel(PageDiscoveryDetails[] discoveryDetails)
    {
        var compositeDisposable = new CompositeDisposable();

        _sectionSource.Edit(list =>
        {
            var toAdd = discoveryDetails
                .GroupBy(x => x.SectionName)
                .Select(group =>
                {
                    var vm = (INewTabPageSectionViewModel)new NewTabPageSectionViewModel(
                        group.Key,
                        group.ToArray()
                    );

                    vm.Activator.Activate().DisposeWith(compositeDisposable);
                    return vm;
                });

            list.AddRange(toAdd);
        });

        _sectionSource
            .Connect()
            .Bind(out _sections)
            .Subscribe();

        this.WhenActivated(disposables =>
        {
            _sectionSource
                .Connect()
                .MergeMany(item => item.SelectItemCommand)
                .Do(_ => Console.WriteLine("tab page view model"))
                .InvokeCommand(ChangePageCommand)
                .DisposeWith(disposables);

            compositeDisposable.DisposeWith(disposables);
        });
    }
}
