using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DevelopmentBuildBanner;

public class DevelopmentBuildBannerViewModel : AViewModel<IDevelopmentBuildBannerViewModel>, IDevelopmentBuildBannerViewModel
{
    private const string GiveFeedbackUrl = "https://forms.gle/krXTRJLhiJM167oG9";
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
