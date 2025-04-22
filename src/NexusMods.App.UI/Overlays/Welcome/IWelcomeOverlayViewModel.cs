using R3;

namespace NexusMods.App.UI.Overlays;

public interface IWelcomeOverlayViewModel : IOverlayViewModel
{
    ReactiveCommand CommandOpenDiscord { get; }
    ReactiveCommand CommandOpenForum { get; }
    ReactiveCommand CommandOpenGitHub { get; }
    ReactiveCommand CommandOpenPrivacyPolicy { get; }

    ReactiveCommand<Unit> CommandLogIn { get; }
    ReactiveCommand<Unit> CommandLogOut { get; }
    IReadOnlyBindableReactiveProperty<bool> IsLoggedIn { get; }

    BindableReactiveProperty<bool> AllowTelemetry { get; }

    ReactiveCommand CommandClose { get; }
}
