using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DevelopmentBuildBanner;

public class DevelopmentBuildBannerViewModel : AViewModel<IDevelopmentBuildBannerViewModel>, IDevelopmentBuildBannerViewModel
{
    private static readonly Uri GiveFeedbackUri = new("https://forms.gle/krXTRJLhiJM167oG9");

    public ReactiveCommand<Unit, Unit> GiveFeedbackCommand { get; }
    public DevelopmentBuildBannerViewModel(IOSInterop osInterop)
    {
        GiveFeedbackCommand = ReactiveCommand.Create(() => osInterop.OpenUri(GiveFeedbackUri));
    }
}
