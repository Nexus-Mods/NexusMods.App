using NexusMods.Abstractions.Telemetry;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.CrossPlatform.Process;
using R3;

namespace NexusMods.App.UI.Overlays;

public interface IUpgradeToPremiumViewModel : IOverlayViewModel
{

    public ReactiveCommand<Unit> CommandCancel { get; }
    
    public ReactiveCommand<Unit> CommandLearnMore { get; }

    public ReactiveCommand<Unit> CommandUpgrade { get; }
}

public class UpgradeToPremiumViewModel : AOverlayViewModel<IUpgradeToPremiumViewModel>, IUpgradeToPremiumViewModel
{
    public UpgradeToPremiumViewModel(
        IOSInterop osInterop)
    {
        CommandCancel = new ReactiveCommand(_ => base.Close());
        
        CommandLearnMore = new ReactiveCommand(
            executeAsync: async (_, cancellationToken) => await osInterop.OpenUrl(NexusModsUrlBuilder.LearnAboutPremiumUri, cancellationToken: cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            configureAwait: false
        );
        
        CommandUpgrade = new ReactiveCommand(
            executeAsync: async (_, cancellationToken) => await osInterop.OpenUrl(NexusModsUrlBuilder.UpgradeToPremiumUri, cancellationToken: cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            configureAwait: false
        );
    }

    public ReactiveCommand<Unit> CommandCancel { get; }
    public ReactiveCommand<Unit> CommandLearnMore { get; }
    public ReactiveCommand<Unit> CommandUpgrade { get; }
}
