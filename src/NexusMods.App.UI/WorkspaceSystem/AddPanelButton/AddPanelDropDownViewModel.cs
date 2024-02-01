using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using DynamicData;
using NexusMods.App.UI.Windows;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class AddPanelDropDownViewModel : AViewModel<IAddPanelDropDownViewModel>, IAddPanelDropDownViewModel
{
    public ReadOnlyObservableCollection<IAddPanelButtonViewModel> AddPanelIconViewModels => _addPanelButtonViewModels;

    private readonly ReadOnlyObservableCollection<IAddPanelButtonViewModel> _addPanelButtonViewModels;

    private readonly SourceList<IAddPanelButtonViewModel> _addPanelIconViewModels = new();
    public int SelectedIndex { get; set; }

    public AddPanelDropDownViewModel(IWorkspaceController workspaceController)
    {
        _addPanelIconViewModels
            .Connect()
            .Bind(out _addPanelButtonViewModels)
            .Subscribe();

        this.WhenActivated(disposables =>
        {
            workspaceController
                .WhenAnyValue(controller => controller.ActiveWorkspace)
                .SubscribeWithErrorLogging(workspace =>
                {
                    // TODO: move this into the Workspace
                    var currentState = WorkspaceGridState.From(workspace.Panels, workspace.IsHorizontal);
                    UpdateDropDownContents(currentState, 2, 2);
                }).DisposeWith(disposables);

            _addPanelIconViewModels.Connect()
                .MergeMany(buttonVm => buttonVm.AddPanelCommand)
                .Subscribe(nextGridState =>
                {
                    workspaceController.AddPanel(
                        workspaceController.ActiveWorkspace.Id,
                        nextGridState,
                        new AddPanelBehavior(new AddPanelBehavior.WithDefaultTab())
                    );
                })
                .DisposeWith(disposables);
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
}
