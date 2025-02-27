using System.Reactive;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DevelopmentBuildBanner;

public interface IDevelopmentBuildBannerViewModel : IViewModelInterface
{
    public ReactiveCommand<Unit, Unit> GiveFeedbackCommand { get; }
}
