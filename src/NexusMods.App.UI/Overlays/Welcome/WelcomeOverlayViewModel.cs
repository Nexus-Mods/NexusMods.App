using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.UI.Settings;
using NexusMods.CrossPlatform.Process;
using R3;
using ReactiveUI;
using ReactiveCommand = R3.ReactiveCommand;

namespace NexusMods.App.UI.Overlays;

public class WelcomeOverlayViewModel : AOverlayViewModel<IWelcomeOverlayViewModel>, IWelcomeOverlayViewModel
{
    public ReactiveCommand CommandOpenDiscord { get; }
    public ReactiveCommand CommandOpenForum { get; }
    public ReactiveCommand CommandOpenGitHub { get; }
    public ReactiveCommand CommandOpenPrivacyPolicy { get; }

    public ReactiveCommand<Unit> CommandLogIn { get; }
    public ReactiveCommand<Unit> CommandLogOut { get; }

    private readonly BindableReactiveProperty<bool> _isLoggedIn = new();
    public IReadOnlyBindableReactiveProperty<bool> IsLoggedIn => _isLoggedIn;

    public BindableReactiveProperty<bool> AllowTelemetry { get; }

    public ReactiveCommand CommandClose { get; }

    public WelcomeOverlayViewModel(
        IOSInterop osInterop,
        ISettingsManager settingsManager,
        ILoginManager loginManager)
    {
        AllowTelemetry = new BindableReactiveProperty<bool>(value: settingsManager.Get<TelemetrySettings>().IsEnabled);

        CommandOpenDiscord = new ReactiveCommand(_ => osInterop.OpenUrl(ConstantLinks.DiscordUri));
        CommandOpenForum = new ReactiveCommand(_ => osInterop.OpenUrl(ConstantLinks.ForumsUri));
        CommandOpenGitHub = new ReactiveCommand(_ => osInterop.OpenUrl(ConstantLinks.GitHubUri));
        CommandOpenPrivacyPolicy = new ReactiveCommand(_ => osInterop.OpenUrl(ConstantLinks.PrivacyPolicyUri));

        CommandLogIn = IsLoggedIn.AsObservable().Select(static isLoggedIn => !isLoggedIn).ToReactiveCommand<Unit>(
            executeAsync: async (_, cancellationToken) => await loginManager.LoginAsync(token: cancellationToken),
            initialCanExecute: false
        );

        CommandLogOut = IsLoggedIn.AsObservable().ToReactiveCommand<Unit>(
            executeAsync: async (_, _) => await loginManager.Logout(),
            initialCanExecute: false
        );

        CommandClose = new ReactiveCommand(_ =>
        {
            settingsManager.Update<TelemetrySettings>(telemetrySettings => telemetrySettings with
            {
                IsEnabled = AllowTelemetry.Value,
            });

            base.Close();
        });

        this.WhenActivated(disposables =>
        {
            loginManager.IsLoggedInObservable
                .ToObservable()
                .ObserveOnUIThreadDispatcher()
                .Subscribe(_isLoggedIn, static (value, property) => property.Value = value)
                .AddTo(disposables);
        });
    }

    public static IWelcomeOverlayViewModel? CreateIfNeeded(IServiceProvider serviceProvider)
    {
        var settingsManager = serviceProvider.GetRequiredService<ISettingsManager>();
        if (settingsManager.Get<WelcomeSettings>().HasShownWelcomeMessage) return null;

        settingsManager.Update<WelcomeSettings>(settings => settings with
        {
            HasShownWelcomeMessage = true,
        });

        return new WelcomeOverlayViewModel(
            osInterop: serviceProvider.GetRequiredService<IOSInterop>(),
            settingsManager: settingsManager,
            loginManager: serviceProvider.GetRequiredService<ILoginManager>()
        );
    }
}
