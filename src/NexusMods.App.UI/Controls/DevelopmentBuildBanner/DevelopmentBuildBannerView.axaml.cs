using Avalonia;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.BuildInfo;

namespace NexusMods.App.UI.Controls.DevelopmentBuildBanner;

[UsedImplicitly]
public partial class DevelopmentBuildBannerView : ReactiveUserControl<IDevelopmentBuildBannerViewModel>
{
    private static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<DevelopmentBuildBannerView, string>(nameof(Text), "vX.X - DEVELOPMENT USE ONLY");

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public DevelopmentBuildBannerView()
    {
        InitializeComponent();

        var appVersion = GetAppVersion();
        Text = $"{appVersion} - DEVELOPMENT USE ONLY";
    }

    private static string GetAppVersion()
    {
        var prefix = CompileConstants.IsDebug ? "Debug build" : ApplicationConstants.Version.ToString();
        return $"{prefix} - {ApplicationConstants.CommitHash}";
    }
}
