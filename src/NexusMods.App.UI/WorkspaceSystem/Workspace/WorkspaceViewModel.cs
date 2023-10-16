using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using Avalonia;
using DynamicData;
using NexusMods.App.UI.Controls;
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
    public IReadOnlyList<IAddPanelButtonViewModel> AddPanelButtonViewModels { get; private set; } = Array.Empty<IAddPanelButtonViewModel>();

    public WorkspaceViewModel()
    {
        this.WhenActivated(disposables =>
        {
            _panelSource
                .Connect()
                .Sort(PanelComparer.Instance)
                .Bind(out _panels)
                .SubscribeWithErrorLogging(_ => UpdateStates())
                .DisposeWith(disposables);
        });
    }

    private Size _lastWorkspaceSize;
    private bool IsHorizontal => _lastWorkspaceSize.Width > _lastWorkspaceSize.Height;

    /// <inheritdoc/>
    public void ArrangePanels(Size workspaceSize)
    {
        _lastWorkspaceSize = workspaceSize;
        foreach (var panelViewModel in _panelSource.Items)
        {
            panelViewModel.Arrange(workspaceSize);
        }
    }

    /// <inheritdoc/>
    public void SwapPanels(IPanelViewModel first, IPanelViewModel second)
    {
        (second.LogicalBounds, first.LogicalBounds) = (first.LogicalBounds, second.LogicalBounds);
        ArrangePanels(_lastWorkspaceSize);
        UpdateStates();
    }

    private void UpdateStates()
    {
        if (_panels.Count == MaxPanelCount)
        {
            AddPanelButtonViewModels = Array.Empty<IAddPanelButtonViewModel>();
            return;
        }

        var states = GridUtils.GetPossibleStates(_panels, Columns, Rows);
        AddPanelButtonViewModels = states.Select(state =>
        {
            var image = IconUtils.StateToBitmap(state);
            return new AddPanelButtonViewModel(this, state, image);
        }).ToArray();
    }

    /// <inheritdoc/>
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
                    panelViewModel = new PanelViewModel(this)
                    {
                        LogicalBounds = logicalBounds,
                    };

                    var tab = panelViewModel.AddTab();
                    tab.Contents = new DummyViewModel();

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

    /// <inheritdoc/>
    public void ClosePanel(IPanelViewModel currentPanel)
    {
        var currentState = _panels.ToImmutableDictionary(panel => panel.Id, panel => panel.LogicalBounds);
        var newState = GridUtils.GetStateWithoutPanel(currentState, currentPanel.Id, isHorizontal: IsHorizontal);

        _panelSource.Edit(updater =>
        {
            updater.Remove(currentPanel.Id);

            foreach (var kv in newState)
            {
                var (panelId, logicalBounds) = kv;
                {
                    var existingPanel = updater.Lookup(panelId);
                    Debug.Assert(existingPanel.HasValue);

                    var value = existingPanel.Value;
                    value.LogicalBounds = logicalBounds;
                    updater.AddOrUpdate(value);
                }
            }
        });
    }
}
