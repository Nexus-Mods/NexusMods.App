using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspacePlaygroundViewModel{}

public class WorkspacePlaygroundViewModel : AViewModel<IWorkspacePlaygroundViewModel>, IWorkspacePlaygroundViewModel
{
    public readonly IWorkspaceViewModel WorkspaceViewModel = new WorkspaceViewModel();

    [Reactive] public ReactiveCommand<Unit, Unit> AddPanelCommand { get; private set; } = Initializers.DisabledReactiveCommand;
    [Reactive] public ReactiveCommand<Unit, Unit> RemovePanelCommand { get; private set; }= Initializers.DisabledReactiveCommand;

    public WorkspacePlaygroundViewModel()
    {
        this.WhenActivated(disposables =>
        {
            WorkspaceViewModel.AddPanel(new Dictionary<PanelId, Rect>
            {
                { PanelId.Empty, MathUtils.One }
            });

            var canAddPanel = this.WhenAnyValue(vm => vm.WorkspaceViewModel.CanAddPanel);
            AddPanelCommand = ReactiveCommand.Create(() =>
            {
                var state = WorkspaceViewModel.PossibleStates[0];
                WorkspaceViewModel.AddPanel(state);
            }, canAddPanel).DisposeWith(disposables);

            var canRemovePanel = this.WhenAnyValue(vm => vm.WorkspaceViewModel.CanRemovePanel);
            RemovePanelCommand = ReactiveCommand.Create(() =>
            {
                var last = WorkspaceViewModel.Panels.TakeLast(2).ToArray();
                var toConsume = last[1];
                var toExpand = last[0];
                WorkspaceViewModel.RemovePanel(new RemovePanelInput(toConsume, toExpand));
            }, canRemovePanel).DisposeWith(disposables);
        });
    }
}
