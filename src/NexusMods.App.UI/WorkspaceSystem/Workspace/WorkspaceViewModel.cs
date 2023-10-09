using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.ReactiveUI;
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

    [Reactive] public bool CanAddPanel { get; private set; }

    [Reactive] public bool CanRemovePanel { get; private set; }

    public WorkspaceViewModel()
    {
        this.WhenActivated(disposables =>
        {
            _panelSource
                .Connect()
                .Sort(PanelComparer.Instance)
                .Bind(out _panels)
                .SubscribeWithErrorLogging(_ =>
                {
                    PossibleStates = GridUtils.GetPossibleStates(_panels, Columns, Rows).ToArray();
                    StateImages = PossibleStates.Select(IconUtils.StateToBitmap).ToArray();
                })
                .DisposeWith(disposables);

            var panelCountObservable = _panelSource.CountChanged;
            var stateCountObservable = this
                .WhenAnyValue(vm => vm.PossibleStates)
                .Select(states => states.Count);

            panelCountObservable
                .CombineLatest(stateCountObservable)
                .Select(tuple =>
                {
                    var (panelCount, stateCount) = tuple;
                    return panelCount < MaxPanelCount && stateCount > 0;
                })
                .SubscribeWithErrorLogging(canAddPanel => CanAddPanel = canAddPanel)
                .DisposeWith(disposables);

            panelCountObservable
                .Select(panelCount => panelCount > 1)
                .SubscribeWithErrorLogging(canRemovePanel => CanRemovePanel = canRemovePanel)
                .DisposeWith(disposables);
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

    public IPanelViewModel AddPanel(IReadOnlyDictionary<PanelId, Rect> state)
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
    }

    public void RemovePanel(RemovePanelInput removePanelInput)
    {
        var (toConsume, toExpand) = removePanelInput;

        // NOTE(erri120): The instruction ordering is important, as the state calculation
        // requires the latest LogicalBounds value.
        toExpand.LogicalBounds = MathUtils.Join(toExpand.LogicalBounds, toConsume.LogicalBounds);
        _panelSource.RemoveKey(toConsume.Id);

        ArrangePanels(_lastWorkspaceSize);
    }
}
