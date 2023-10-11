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

    [Reactive] public ReactiveCommand<Unit, Unit> SwapPanelsCommand { get; private set; } = Initializers.DisabledReactiveCommand;

    public WorkspacePlaygroundViewModel()
    {
        this.WhenActivated(disposables =>
        {
            WorkspaceViewModel.AddPanel(new Dictionary<PanelId, Rect>
            {
                { PanelId.Empty, MathUtils.One }
            });

            SwapPanelsCommand = ReactiveCommand.Create(() =>
            {
                var panels = WorkspaceViewModel.Panels.OrderBy(_ => Guid.NewGuid()).Take(2).ToArray();
                WorkspaceViewModel.SwapPanels(panels[0], panels[1]);
            }).DisposeWith(disposables);
        });
    }
}
