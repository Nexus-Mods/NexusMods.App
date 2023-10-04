using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspacePlaygroundViewModel{}

public class WorkspacePlaygroundViewModel : AViewModel<IWorkspacePlaygroundViewModel>, IWorkspacePlaygroundViewModel
{
    public readonly WorkspaceViewModel WorkspaceViewModel = new();

    [Reactive]
    public bool SplitVertically { get; set; }

    public WorkspacePlaygroundViewModel()
    {
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.SplitVertically)
                .SubscribeWithErrorLogging(value => WorkspaceViewModel.SplitVertically = value)
                .DisposeWith(disposables);
        });
    }
}
