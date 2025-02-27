using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DevelopmentBuildBanner;

public class DevelopmentBuildBannerDesignViewModel : AViewModel<IDevelopmentBuildBannerViewModel>, IDevelopmentBuildBannerViewModel
{
    public ReactiveCommand<Unit, Unit> GiveFeedbackCommand { get; } = ReactiveCommand.Create(() => { });

    public DevelopmentBuildBannerDesignViewModel() { }
}
