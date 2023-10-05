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
    public readonly WorkspaceViewModel WorkspaceViewModel = new();

    [Reactive] public ReactiveCommand<Unit, Unit> AddPanelCommand { get; private set; } = Initializers.DisabledReactiveCommand;
    [Reactive] public ReactiveCommand<Unit, Unit> RemovePanelCommand { get; private set; }= Initializers.DisabledReactiveCommand;

    public WorkspacePlaygroundViewModel()
    {
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.WorkspaceViewModel.AddPanelCommand)
                .SubscribeWithErrorLogging(cmd => cmd.Execute(new Dictionary<PanelId, Rect>
                {
                    { PanelId.Empty, MathUtils.One }
                }).Wait())
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.WorkspaceViewModel.AddPanelCommand)
                .SubscribeWithErrorLogging(cmd =>
                {
                    AddPanelCommand = ReactiveCommand.Create(() =>
                    {
                        var state = WorkspaceViewModel.PossibleStates.First();
                        WorkspaceViewModel.AddPanelCommand.Execute(state).Wait();
                    }, cmd.CanExecute).DisposeWith(disposables);
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.WorkspaceViewModel.RemovePanelCommand)
                .SubscribeWithErrorLogging(cmd =>
                {
                    RemovePanelCommand = ReactiveCommand.Create(() =>
                    {
                        var last = WorkspaceViewModel.Panels.TakeLast(2).ToArray();
                        var toConsume = last[1];
                        var toExpand = last[0];
                        WorkspaceViewModel.RemovePanelCommand.Execute(new RemovePanelInput(toConsume, toExpand)).Wait();
                    }, cmd.CanExecute).DisposeWith(disposables);
                })
                .DisposeWith(disposables);
        });
    }
}
