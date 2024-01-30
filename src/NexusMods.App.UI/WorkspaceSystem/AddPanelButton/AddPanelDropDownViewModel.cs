using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia;
using DynamicData;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class AddPanelDropDownViewModel : AViewModel<IAddPanelDropDownViewModel>, IAddPanelDropDownViewModel
{
    public ReadOnlyObservableCollection<IAddPanelButtonViewModel> AddPanelIconViewModels => _addPanelButtonViewModels;

    private readonly ReadOnlyObservableCollection<IAddPanelButtonViewModel> _addPanelButtonViewModels;

    private readonly SourceList<IAddPanelButtonViewModel> _addPanelIconViewModels = new();
    public int SelectedIndex { get; set; }

    public AddPanelDropDownViewModel()
    {
        _addPanelIconViewModels
            .Connect()
            .Bind(out _addPanelButtonViewModels)
            .Subscribe();

        // TODO: Get the current WorkspaceGridState from the WorkspaceController.ActiveWorkspace
        UpdateDropDownContents(DummyTwoVerticalPanels, 2, 2);

        this.WhenActivated(d =>
        {
            _addPanelIconViewModels.Connect()
                .MergeMany(buttonVm => buttonVm.AddPanelCommand)
                .Subscribe(nextGridState =>
                {
                    // TODO: Use the nextGridState to update the WorkspaceController.ActiveWorkspace to add a new panel
                })
                .DisposeWith(d);
        });
    }

    private void UpdateDropDownContents(WorkspaceGridState currentState, int maxColumns, int maxRows)
    {
        _addPanelIconViewModels.Edit(updater =>
        {
            updater.Clear();

            var newStates = GridUtils.GetPossibleStates(currentState, maxColumns, maxRows);

            foreach (var state in newStates)
            {
                var image = IconUtils.StateToBitmap(state);
                updater.Add(new AddPanelButtonViewModel(state, image));
            }
        });
        SelectedIndex = 0;
    }

    //TODO: Remove this when the real implementation is done
    private static readonly WorkspaceGridState DummySinglePanel = WorkspaceGridState.From(
        isHorizontal: true,
        new PanelGridState(PanelId.NewId(), new Rect(0, 0, 1, 1))
    );

    //TODO: Remove this when the real implementation is done
    private static readonly WorkspaceGridState DummyTwoVerticalPanels = WorkspaceGridState.From(
        isHorizontal: true,
        new PanelGridState(PanelId.NewId(), new Rect(0, 0, 0.5, 1)),
        new PanelGridState(PanelId.NewId(), new Rect(0.5, 0, 0.5, 1))
    );

    //TODO: Remove this when the real implementation is done
    private static readonly WorkspaceGridState DummyTwoHorizontalPanels = WorkspaceGridState.From(
        isHorizontal: true,
        new PanelGridState(PanelId.NewId(), new Rect(0, 0, 1, 0.5)),
        new PanelGridState(PanelId.NewId(), new Rect(0, 0.5, 1, 0.5))
    );

    //TODO: Remove this when the real implementation is done
    private static readonly WorkspaceGridState DummyThreePanels = WorkspaceGridState.From(
        isHorizontal: true,
        new PanelGridState(PanelId.NewId(), new Rect(0, 0, 0.5, 1)),
        new PanelGridState(PanelId.NewId(), new Rect(0.5, 0, 0.5, 0.5)),
        new PanelGridState(PanelId.NewId(), new Rect(0.5, 0.5, 0.5, 0.5))
    );
}
