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

    public readonly ReactiveCommand<Unit, Unit> SaveWorkspaceCommand;
    public readonly ReactiveCommand<Unit, Unit> LoadWorkspaceCommand;

    [Reactive] private WorkspaceData? SavedWorkspaceData { get; set; }

    public WorkspacePlaygroundViewModel()
    {
        SaveWorkspaceCommand = ReactiveCommand.Create(() =>
        {
            SavedWorkspaceData = WorkspaceViewModel.ToData();
        });

        LoadWorkspaceCommand = ReactiveCommand.Create(() =>
        {
            WorkspaceViewModel.FromData(SavedWorkspaceData!);
        }, this.WhenAnyValue(vm => vm.SavedWorkspaceData).Select(data => data is not null));

        this.WhenActivated(disposables =>
        {
            WorkspaceViewModel.AddPanel(new Dictionary<PanelId, Rect>
            {
                { PanelId.Empty, MathUtils.One }
            });

            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }
}
