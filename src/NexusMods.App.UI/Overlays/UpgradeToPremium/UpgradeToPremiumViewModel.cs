using NexusMods.Abstractions.Telemetry;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.CrossPlatform.Process;
using R3;

namespace NexusMods.App.UI.Overlays;

public interface IUpgradeToPremiumViewModel : IOverlayViewModel
{
    public IMarkdownRendererViewModel MarkdownRendererViewModel { get; }

    public ReactiveCommand<Unit> CommandCancel { get; }

    public ReactiveCommand<Unit> CommandUpgrade { get; }
}

public class UpgradeToPremiumViewModel : AOverlayViewModel<IUpgradeToPremiumViewModel>, IUpgradeToPremiumViewModel
{
    public UpgradeToPremiumViewModel(
        IOSInterop osInterop,
        IMarkdownRendererViewModel markdownRendererViewModel)
    {
        MarkdownRendererViewModel = markdownRendererViewModel;
        MarkdownRendererViewModel.Contents = $"""
### Upgrade to Premium to unlock 1 click downloads

Download all mods in a collection at full speed with 1 click and without leaving the app.
No more visiting every mod page in the browser.

Premium users also get:

* **Uncapped downloads** - Download all mods without any speed limits.
* **No Ads - For Life!** - Never see ads again on the website, even if you cancel!
* **Support authors** - Premium memberships allow Nexus Mods to reward mod authors.

[Learn more about Premium]({ NexusModsUrlBuilder.LearAboutPremiumUri })
""";

        CommandCancel = new ReactiveCommand(_ => base.Close());
        CommandUpgrade = new ReactiveCommand(
            executeAsync: async (_, cancellationToken) => await osInterop.OpenUrl(NexusModsUrlBuilder.UpgradeToPremiumUri, cancellationToken: cancellationToken),
            awaitOperation: AwaitOperation.Parallel,
            configureAwait: false
        );
    }

    public IMarkdownRendererViewModel MarkdownRendererViewModel { get; }
    public ReactiveCommand<Unit> CommandCancel { get; }
    public ReactiveCommand<Unit> CommandUpgrade { get; }
}
