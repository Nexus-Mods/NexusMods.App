using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Windows;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IWorkspacePlaygroundViewModel : IViewModelInterface;

public class WorkspacePlaygroundViewModel : AViewModel<IWorkspacePlaygroundViewModel>, IWorkspacePlaygroundViewModel
{
    public readonly IWorkspaceViewModel WorkspaceViewModel;

    public readonly ReactiveCommand<Unit, Unit> SaveWorkspaceCommand;
    public readonly ReactiveCommand<Unit, Unit> LoadWorkspaceCommand;

    [Reactive] private string? SavedWorkspaceData { get; set; }

    public WorkspacePlaygroundViewModel()
    {
        var serviceProvider = DesignerUtils.GetServiceProvider();

        var workspaceController = serviceProvider.GetRequiredService<IWorkspaceController>();
        var factoryController = serviceProvider.GetRequiredService<PageFactoryController>();
        var jsonSerializerOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();

        WorkspaceViewModel = new WorkspaceViewModel(workspaceController, factoryController);

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
            workspaceController.AddPanel(
                WorkspaceViewModel.Id,
                WorkspaceGridState.From(new[]
                {
                    new PanelGridState(PanelId.DefaultValue, MathUtils.One)
                }, isHorizontal: WorkspaceViewModel.IsHorizontal),
                new AddPanelBehavior(new AddPanelBehavior.WithDefaultTab())
            );

            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }
}
