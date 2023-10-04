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

    private IPanelViewModel? _topLeftPanel;
    private IPanelViewModel? _topRightPanel;
    private IPanelViewModel? _bottomLeftPanel;
    private IPanelViewModel? _bottomRightPanel;

    public WorkspacePlaygroundViewModel()
    {
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.WorkspaceViewModel.AddPanelCommand)
                .SubscribeWithErrorLogging(cmd =>
                {
                    _topLeftPanel = cmd.Execute(new AddPanelInput
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
                    _topRightPanel = WorkspaceViewModel.AddPanelCommand.Execute(new AddPanelInput
                    {
                        PanelToSplit = _topLeftPanel,
                        SplitVertically = true
                    }).Wait();
                }
                else
                {
                    throw new NotImplementedException();
                }

                HasTopRightPanel = !HasTopRightPanel;
            }, canToggleTopRightPanel).DisposeWith(disposables);

            var canToggleBottomLeftPanel = this.WhenAnyValue(vm => vm.HasTopLeftPanel);
            ToggleBottomLeftPanel = ReactiveCommand.Create(() =>
            {
                if (!HasBottomLeftPanel)
                {
                    _bottomLeftPanel = WorkspaceViewModel.AddPanelCommand.Execute(new AddPanelInput
                    {
                        PanelToSplit = _topLeftPanel,
                        SplitVertically = false
                    }).Wait();
                }
                else
                {
                    throw new NotImplementedException();
                }

                HasBottomLeftPanel = !HasBottomLeftPanel;
            }, canToggleBottomLeftPanel).DisposeWith(disposables);

            var canToggleBottomRightPanel = this.WhenAnyValue(vm => vm.HasTopRightPanel, vm => vm.HasBottomLeftPanel, (hasTopRightPanel, hasBottomLeftPanel) => hasTopRightPanel || hasBottomLeftPanel);
            ToggleBottomRightPanel = ReactiveCommand.Create(() =>
            {
                if (!HasBottomRightPanel)
                {
                    _bottomRightPanel = WorkspaceViewModel.AddPanelCommand.Execute(new AddPanelInput
                    {
                        PanelToSplit = HasBottomLeftPanel ? _bottomLeftPanel : _topRightPanel,
                        SplitVertically = HasBottomLeftPanel
                    }).Wait();
                }
                else
                {
                    throw new NotImplementedException();
                }

                HasBottomRightPanel = !HasBottomRightPanel;
            }, canToggleBottomRightPanel).DisposeWith(disposables);
        });
    }
}
