using System.Reactive;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DevelopmentBuildBanner;

public interface IDevelopmentBuildBannerViewModel : IViewModelInterface
{
    public ReactiveCommand<Unit, Unit> GiveFeedbackCommand { get; }
}
