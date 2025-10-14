using System.Reactive;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DevelopmentBuildBanner;

public class DevelopmentBuildBannerDesignViewModel : AViewModel<IDevelopmentBuildBannerViewModel>, IDevelopmentBuildBannerViewModel
{
    public ReactiveCommand<Unit, Unit> GiveFeedbackCommand { get; } = ReactiveCommand.Create(() => { });

    public DevelopmentBuildBannerDesignViewModel() { }
}
