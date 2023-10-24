using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspacePlaygroundViewModel{}

public class WorkspacePlaygroundViewModel : AViewModel<IWorkspacePlaygroundViewModel>, IWorkspacePlaygroundViewModel
{
    public readonly IWorkspaceViewModel WorkspaceViewModel = new WorkspaceViewModel(StaticServiceProvider.Get().GetRequiredService<PageFactoryController>());

    public readonly ReactiveCommand<Unit, Unit> SaveWorkspaceCommand;
    public readonly ReactiveCommand<Unit, Unit> LoadWorkspaceCommand;

    [Reactive] private string? SavedWorkspaceData { get; set; }

    public WorkspacePlaygroundViewModel()
    {
        var jsonSerializerOptions = StaticServiceProvider.Get().GetRequiredService<JsonSerializerOptions>();

        SaveWorkspaceCommand = ReactiveCommand.Create(() =>
        {
            var workspaceData = WorkspaceViewModel.ToData();

            try
            {
                var res = JsonSerializer.Serialize(workspaceData, jsonSerializerOptions);
                SavedWorkspaceData = res;
                Console.WriteLine(SavedWorkspaceData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });

        LoadWorkspaceCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                var workspaceData = JsonSerializer.Deserialize<WorkspaceData>(SavedWorkspaceData!, jsonSerializerOptions);
                WorkspaceViewModel.FromData(workspaceData!);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

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
