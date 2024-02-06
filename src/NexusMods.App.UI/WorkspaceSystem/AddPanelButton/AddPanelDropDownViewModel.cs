using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class AddPanelDropDownViewModel : AViewModel<IAddPanelDropDownViewModel>, IAddPanelDropDownViewModel
{
    private readonly ObservableCollectionExtended<IAddPanelButtonViewModel> _addPanelButtonViewModels = new();
    public IReadOnlyList<IAddPanelButtonViewModel> AddPanelButtonViewModels => _addPanelButtonViewModels;

    [Reactive] public IAddPanelButtonViewModel? SelectedItem { get; set; }

    [Reactive] public int SelectedIndex { get; set; } = -1;

    public AddPanelDropDownViewModel(IWorkspaceController workspaceController)
    {
        this.WhenActivated(disposables =>
        {
            var serialDisposable = new SerialDisposable();
            serialDisposable.DisposeWith(disposables);

            workspaceController
                .WhenAnyValue(controller => controller.ActiveWorkspace)
                .SubscribeWithErrorLogging(activeWorkspace =>
                {
                    // clear the collection when changing workspaces to avoid having
                    // buttons for different workspaces in the collection
                    _addPanelButtonViewModels.Clear();

                    if (activeWorkspace is null)
                    {
                        serialDisposable.Disposable = null;
                        SelectedIndex = -1;
                        return;
                    }

                    // subscribes to changes to the observable collection and applies all
                    // changes to the observable collection
                    serialDisposable.Disposable = activeWorkspace.AddPanelButtonViewModels
                        .ToObservableChangeSet()
                        .Adapt(new ObservableCollectionAdaptor<IAddPanelButtonViewModel>(_addPanelButtonViewModels))
                        .SubscribeWithErrorLogging(_ =>
                        {
                            SelectedIndex = _addPanelButtonViewModels.Count == 0 ? -1 : 0;
                        });
                })
                .DisposeWith(disposables);
        });
    }
}
