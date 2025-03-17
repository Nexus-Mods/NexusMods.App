using NexusMods.Abstractions.GameLocators;
using R3;

namespace NexusMods.App.UI.Overlays;

public class RemoveGameOverlayViewModel : AOverlayViewModel<IRemoveGameOverlayViewModel, RemoveGameOverlayResult>, IRemoveGameOverlayViewModel
{
    public string GameName { get; }
    public BindableReactiveProperty<bool> ShouldDeleteDownloads { get; } = new(value: false);
    public ReactiveCommand<Unit> CommandCancel { get; }
    public ReactiveCommand<Unit> CommandRemove { get; }

    public RemoveGameOverlayViewModel(GameInstallation gameInstallation)
    {
        GameName = gameInstallation.Game.Name;

        CommandCancel = new ReactiveCommand(_ => Complete(result: RemoveGameOverlayResult.Cancel));
        CommandRemove = new ReactiveCommand(_ => Complete(result: new RemoveGameOverlayResult(ShouldRemoveGame: true, ShouldDeleteDownloads: ShouldDeleteDownloads.Value)));
    }
}
