using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Overlays;

public class RemoveGameOverlayViewModel : AOverlayViewModel<IRemoveGameOverlayViewModel, RemoveGameOverlayResult>, IRemoveGameOverlayViewModel
{
    public required string GameName { get; init; }
    public required int NumDownloads { get; init; }
    public required Size SumDownloadsSize { get; init; }
    public required int NumCollections { get; init; }
    public BindableReactiveProperty<bool> ShouldDeleteDownloads { get; } = new(value: false);
    public ReactiveCommand<Unit> CommandCancel { get; }
    public ReactiveCommand<Unit> CommandRemove { get; }

    public RemoveGameOverlayViewModel()
    {
        CommandCancel = new ReactiveCommand(_ => Complete(result: RemoveGameOverlayResult.Cancel));
        CommandRemove = new ReactiveCommand(_ => Complete(result: new RemoveGameOverlayResult(ShouldRemoveGame: true, ShouldDeleteDownloads: ShouldDeleteDownloads.Value)));
    }
}
