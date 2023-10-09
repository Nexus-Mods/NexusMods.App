using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class WorkspaceViewModel : AViewModel<IWorkspaceViewModel>, IWorkspaceViewModel
{
    private const int Columns = 2;
    private const int Rows = 2;
    private const int MaxPanelCount = Columns * Rows;

    private readonly SourceCache<IPanelViewModel, PanelId> _panelSource = new(x => x.Id);

    private ReadOnlyObservableCollection<IPanelViewModel> _panels = Initializers.ReadOnlyObservableCollection<IPanelViewModel>();
    public ReadOnlyObservableCollection<IPanelViewModel> Panels => _panels;

    [Reactive]
    public IReadOnlyList<IReadOnlyDictionary<PanelId, Rect>> PossibleStates { get; private set; } = Array.Empty<IReadOnlyDictionary<PanelId, Rect>>();

    [Reactive]
    public IReadOnlyList<Bitmap> StateImages { get; private set; } = Array.Empty<Bitmap>();

    /// <inheritdoc/>
    [Reactive]
    public ReactiveCommand<IReadOnlyDictionary<PanelId, Rect>, IPanelViewModel> AddPanelCommand { get; private set; } = Initializers.CreateReactiveCommand<IReadOnlyDictionary<PanelId, Rect>, IPanelViewModel>();

    /// <inheritdoc/>
    [Reactive]
    public ReactiveCommand<RemovePanelInput, Unit> RemovePanelCommand { get; private set; } = Initializers.CreateReactiveCommand<RemovePanelInput>();

    public WorkspaceViewModel()
    {
        this.WhenActivated(disposables =>
        {
            _panelSource
                .Connect()
                .Sort(PanelComparer.Instance)
                .Bind(out _panels)
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            _panels
                .ObserveCollectionChanges()
                .SubscribeWithErrorLogging(_ =>
                {
                    PossibleStates = GridUtils.GetPossibleStates(_panels, Columns, Rows).ToArray();
                    StateImages = PossibleStates.Select(IconUtils.StateToBitmap).ToArray();
                })
                .DisposeWith(disposables);

            var canAddPanel = _panelSource.CountChanged.Select(count => count < MaxPanelCount);
            AddPanelCommand = ReactiveCommand.Create<IReadOnlyDictionary<PanelId, Rect>, IPanelViewModel>(state =>
            {
                IPanelViewModel panelViewModel = null!;
                _panelSource.Edit(updater =>
                {
                    foreach (var kv in state)
                    {
                        var (panelId, logicalBounds) = kv;
                        if (panelId == PanelId.Empty)
                        {
                            panelViewModel = new PanelViewModel
                            {
                                LogicalBounds = logicalBounds
                            };

                            panelViewModel.Arrange(_lastWorkspaceSize);
                            updater.AddOrUpdate(panelViewModel);
                        }
                        else
                        {
                            var existingPanel = updater.Lookup(panelId);
                            Debug.Assert(existingPanel.HasValue);

                            var value = existingPanel.Value;
                            value.LogicalBounds = logicalBounds;
                            updater.AddOrUpdate(value);
                        }
                    }
                });

                Debug.Assert(panelViewModel is not null);
                return panelViewModel;
            }, canAddPanel).DisposeWith(disposables);

            var canRemovePanel = _panelSource.CountChanged.Select(count => count > 1);
            RemovePanelCommand = ReactiveCommand.Create<RemovePanelInput, Unit>(removePanelInput =>
            {
                var (toConsume, toExpand) = removePanelInput;
                // Console.WriteLine($"removing: {toConsume.Id} | expanding: {toExpand.Id}");

                // NOTE(erri120): The instruction ordering is important, as the state calculation
                // requires the latest LogicalBounds value.
                toExpand.LogicalBounds = MathUtils.Join(toExpand.LogicalBounds, toConsume.LogicalBounds);
                _panelSource.RemoveKey(toConsume.Id);

                ArrangePanels(_lastWorkspaceSize);
                return Unit.Default;
            }, canRemovePanel);
        });
    }

    private Size _lastWorkspaceSize;
    public void ArrangePanels(Size workspaceSize)
    {
        _lastWorkspaceSize = workspaceSize;
        foreach (var panelViewModel in _panelSource.Items)
        {
            panelViewModel.Arrange(workspaceSize);
        }
    }
}
