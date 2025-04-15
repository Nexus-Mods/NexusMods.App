using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Overlays;

public class RemoveGameOverlayDesignViewModel : AOverlayViewModel<IRemoveGameOverlayViewModel, RemoveGameOverlayResult>, IRemoveGameOverlayViewModel
{
    public required string GameName { get; init; } = "Stardew Valley";
    public required int NumDownloads { get; init; } = 98;
    public required Size SumDownloadsSize { get; init; } = Size.From(3435678);
    public required int NumCollections { get; init; } = 3;
    public BindableReactiveProperty<bool> ShouldDeleteDownloads { get; } = new(value: false);
    public ReactiveCommand<Unit> CommandCancel { get; }
    public ReactiveCommand<Unit> CommandRemove { get; }

    public RemoveGameOverlayDesignViewModel()
    {
        CommandCancel = new ReactiveCommand(_ => Complete(result: RemoveGameOverlayResult.Cancel));
        CommandRemove = new ReactiveCommand(_ => Complete(result: new RemoveGameOverlayResult(ShouldRemoveGame: true, ShouldDeleteDownloads: ShouldDeleteDownloads.Value)));
    }
}
