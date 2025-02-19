using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DevelopmentBuildBanner;

public class DevelopmentBuildBannerViewModel : AViewModel<IDevelopmentBuildBannerViewModel>, IDevelopmentBuildBannerViewModel
{
    // TODO: update this to a hotjar/google form URL when we have it
    private const string GiveFeedbackUrl = "https://github.com/Nexus-Mods/NexusMods.App/issues/new/choose";
    public ReactiveCommand<Unit, Unit> GiveFeedbackCommand { get; }
    public DevelopmentBuildBannerViewModel(IOSInterop osInterop)
    {
        GiveFeedbackCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var uri = new Uri(GiveFeedbackUrl);
            await osInterop.OpenUrl(uri);
        });
    }
}
