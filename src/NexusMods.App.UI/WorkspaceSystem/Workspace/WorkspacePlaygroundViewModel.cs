using System.Reactive.Disposables;
using Avalonia;
using NexusMods.App.UI.Controls;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspacePlaygroundViewModel{}

public class WorkspacePlaygroundViewModel : AViewModel<IWorkspacePlaygroundViewModel>, IWorkspacePlaygroundViewModel
{
    public readonly IWorkspaceViewModel WorkspaceViewModel = new WorkspaceViewModel();

    public WorkspacePlaygroundViewModel()
    {
        this.WhenActivated(disposables =>
        {
            var panel = WorkspaceViewModel.AddPanel(new Dictionary<PanelId, Rect>
            {
                { PanelId.Empty, MathUtils.One }
            });

            var tab = panel.AddTab();
            tab.Contents = new DummyViewModel();

            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }
}
