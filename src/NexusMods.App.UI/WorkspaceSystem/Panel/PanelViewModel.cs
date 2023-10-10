using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia;
using DynamicData;
using NexusMods.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelViewModel : AViewModel<IPanelViewModel>, IPanelViewModel
{
    public PanelId Id { get; } = PanelId.New();

    private readonly SourceCache<IPanelTabViewModel, PanelTabId> _tabsSource = new(x => x.Id);

    private ReadOnlyObservableCollection<IPanelTabViewModel> _tabs = Initializers.ReadOnlyObservableCollection<IPanelTabViewModel>();
    public ReadOnlyObservableCollection<IPanelTabViewModel> Tabs => _tabs;

    [Reactive]
    public IPanelTabViewModel? SelectedTab { get; set; }

    [Reactive]
    public IViewModel? SelectedTabContents { get; private set; }

    /// <inheritdoc/>
    [Reactive] public Rect LogicalBounds { get; set; }

    /// <inheritdoc/>
    [Reactive] public Rect ActualBounds { get; private set; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    public PanelViewModel(IWorkspaceViewModel workspaceViewModel)
    {
        CloseCommand = ReactiveCommand.Create(() =>
        {
            workspaceViewModel.ClosePanel(this);

            SelectedTab = null;
            _tabsSource.Clear();
            _tabsSource.Dispose();
        });

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.LogicalBounds)
                .SubscribeWithErrorLogging(_ => UpdateActualBounds())
                .DisposeWith(disposables);

            _tabsSource
                .Connect()
                .DisposeMany()
                .Sort(PanelTabComparer.Instance)
                .Bind(out _tabs)
                .SubscribeWithErrorLogging(changeSet =>
                {
                    Console.WriteLine($"adds: {changeSet.Adds}");
                    Console.WriteLine($"removes: {changeSet.Removes}");
                    Console.WriteLine($"updates: {changeSet.Updates}");

                    // TODO: handle removals and update indices
                    if (changeSet.TryGetFirst(change => change.Reason == ChangeReason.Add, out var added))
                    {
                        SelectedTab = added.Current;
                    }
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.SelectedTab)
                .SubscribeWithErrorLogging(tab => SelectedTabContents = tab?.Contents)
                .DisposeWith(disposables);
        });
    }

    private Size _workspaceSize = MathUtils.Zero;
    private void UpdateActualBounds()
    {
        ActualBounds = MathUtils.CalculateActualBounds(_workspaceSize, LogicalBounds);
    }

    /// <inheritdoc/>
    public void Arrange(Size workspaceSize)
    {
        _workspaceSize = workspaceSize;
        UpdateActualBounds();
    }

    public IPanelTabViewModel AddTab()
    {
        var nextIndex = _tabs.Count == 0
            ? PanelTabIndex.From(0)
            : PanelTabIndex.From(_tabs.Last().Index.Value + 1);

        var tab = new PanelTabViewModel(nextIndex);
        _tabsSource.AddOrUpdate(tab);
        return tab;
    }
}
