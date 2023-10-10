using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspacePlaygroundViewModel{}

public class WorkspacePlaygroundViewModel : AViewModel<IWorkspacePlaygroundViewModel>, IWorkspacePlaygroundViewModel
{
    public readonly WorkspaceViewModel WorkspaceViewModel = new();

    [Reactive] public ReactiveCommand<Unit, Unit> ToggleTopLeftPanel { get; set; } = Initializers.DisabledReactiveCommand;
    [Reactive] public ReactiveCommand<Unit, Unit> ToggleTopRightPanel { get; set; } = Initializers.DisabledReactiveCommand;
    [Reactive] public ReactiveCommand<Unit, Unit> ToggleBottomLeftPanel { get; set; } = Initializers.DisabledReactiveCommand;
    [Reactive] public ReactiveCommand<Unit, Unit> ToggleBottomRightPanel { get; set; } = Initializers.DisabledReactiveCommand;

    [Reactive] public bool HasTopLeftPanel { get; set; }
    [Reactive] public bool HasTopRightPanel { get; set; }
    [Reactive] public bool HasBottomLeftPanel { get; set; }
    [Reactive] public bool HasBottomRightPanel { get; set; }

    [Reactive] private IPanelViewModel? TopLeftPanel { get; set; }
    [Reactive] private IPanelViewModel? TopRightPanel { get; set; }
    [Reactive] private IPanelViewModel? BottomLeftPanel { get; set; }
    [Reactive] private IPanelViewModel? BottomRightPanel { get; set; }

    public WorkspacePlaygroundViewModel()
    {
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.TopLeftPanel)
                .SubscribeWithErrorLogging(value => HasTopLeftPanel = value is not null)
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.TopRightPanel)
                .SubscribeWithErrorLogging(value => HasTopRightPanel = value is not null)
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.BottomLeftPanel)
                .SubscribeWithErrorLogging(value => HasBottomLeftPanel = value is not null)
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.BottomRightPanel)
                .SubscribeWithErrorLogging(value => HasBottomRightPanel = value is not null)
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.WorkspaceViewModel.AddPanelCommand)
                .SubscribeWithErrorLogging(cmd =>
                {
                    TopLeftPanel = cmd.Execute(new AddPanelInput
                    {
                        PanelToSplit = null,
                        SplitVertically = true
                    }).Wait();
                    HasTopLeftPanel = true;
                })
                .DisposeWith(disposables);

            var canToggleTopRightPanel = this.WhenAnyValue(vm => vm.HasTopLeftPanel);
            ToggleTopRightPanel = ReactiveCommand.Create(() =>
            {
                if (!HasTopRightPanel)
                {
                    TopRightPanel = WorkspaceViewModel.AddPanelCommand.Execute(new AddPanelInput
                    {
                        PanelToSplit = HasBottomRightPanel ? BottomRightPanel : TopLeftPanel,
                        SplitVertically = !HasBottomRightPanel
                    }).Wait();
                }
                else
                {
                    WorkspaceViewModel.RemovePanelCommand.Execute(new RemovePanelInput
                    {
                        PanelToConsume = TopRightPanel!,
                        PanelToExpand = HasBottomRightPanel ? BottomRightPanel! : TopLeftPanel!
                    }).Wait();
                }

                HasTopRightPanel = !HasTopRightPanel;
            }, canToggleTopRightPanel).DisposeWith(disposables);

            var canToggleBottomLeftPanel = this.WhenAnyValue(vm => vm.HasTopLeftPanel);
            ToggleBottomLeftPanel = ReactiveCommand.Create(() =>
            {
                if (!HasBottomLeftPanel)
                {
                    BottomLeftPanel = WorkspaceViewModel.AddPanelCommand.Execute(new AddPanelInput
                    {
                        PanelToSplit = TopLeftPanel,
                        SplitVertically = false
                    }).Wait();
                }
                else
                {
                    WorkspaceViewModel.RemovePanelCommand.Execute(new RemovePanelInput
                    {
                        PanelToConsume = BottomLeftPanel!,
                        PanelToExpand = TopLeftPanel!
                    }).Wait();
                }

                HasBottomLeftPanel = !HasBottomLeftPanel;
            }, canToggleBottomLeftPanel).DisposeWith(disposables);

            var canToggleBottomRightPanel = this.WhenAnyValue(vm => vm.HasTopRightPanel, vm => vm.HasBottomLeftPanel, (hasTopRightPanel, hasBottomLeftPanel) => hasTopRightPanel || hasBottomLeftPanel);
            ToggleBottomRightPanel = ReactiveCommand.Create(() =>
            {
                if (!HasBottomRightPanel)
                {
                    BottomRightPanel = WorkspaceViewModel.AddPanelCommand.Execute(new AddPanelInput
                    {
                        PanelToSplit = HasBottomLeftPanel ? BottomLeftPanel : TopRightPanel,
                        SplitVertically = HasBottomLeftPanel
                    }).Wait();
                }
                else
                {
                    WorkspaceViewModel.RemovePanelCommand.Execute(new RemovePanelInput
                    {
                        PanelToConsume = BottomRightPanel!,
                        PanelToExpand = HasBottomLeftPanel ? BottomLeftPanel! : TopRightPanel!
                    }).Wait();
                }

                HasBottomRightPanel = !HasBottomRightPanel;
            }, canToggleBottomRightPanel).DisposeWith(disposables);
        });
    }
}
