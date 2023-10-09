using System.Reactive;
using System.Reactive.Disposables;
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
    [Reactive] public ReactiveCommand<Unit, Unit> SwapPanelsCommand { get; private set; } = Initializers.DisabledReactiveCommand;

    public WorkspacePlaygroundViewModel()
    {
        this.WhenActivated(disposables =>
        {
            WorkspaceViewModel.AddPanel(new Dictionary<PanelId, Rect>
            {
                { PanelId.Empty, MathUtils.One }
            });

            var canRemovePanel = this.WhenAnyValue(vm => vm.WorkspaceViewModel.CanRemovePanel);
            RemovePanelCommand = ReactiveCommand.Create(() =>
            {
                var last = WorkspaceViewModel.Panels.TakeLast(2).ToArray();
                var toConsume = last[1];
                var toExpand = last[0];
                WorkspaceViewModel.RemovePanel(new RemovePanelInput(toConsume, toExpand));
            }, canRemovePanel).DisposeWith(disposables);

            SwapPanelsCommand = ReactiveCommand.Create(() =>
            {
                var panels = WorkspaceViewModel.Panels.OrderBy(_ => Guid.NewGuid()).Take(2).ToArray();
                WorkspaceViewModel.SwapPanels(panels[0], panels[1]);
            }).DisposeWith(disposables);
        });
    }
}
