using Avalonia;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.BuildInfo;

namespace NexusMods.App.UI.Controls.DevelopmentBuildBanner;

[UsedImplicitly]
public partial class DevelopmentBuildBannerView : ReactiveUserControl<IDevelopmentBuildBannerViewModel>
{
    private static readonly StyledProperty<string> AppNameProperty =
        AvaloniaProperty.Register<DevelopmentBuildBannerView, string>(nameof(AppName), "DEVELOPMENT USE ONLY");
    
    private static readonly StyledProperty<string> AppVersionProperty =
        AvaloniaProperty.Register<DevelopmentBuildBannerView, string>(nameof(AppVersion), "vX.X.X");

    public string AppName
    {
        get => GetValue(AppNameProperty);
        set => SetValue(AppNameProperty, value);
    }
    
    public string AppVersion
    {
        get => GetValue(AppVersionProperty);
        set => SetValue(AppVersionProperty, value);
    }

    public DevelopmentBuildBannerView()
    {
        InitializeComponent();
        
        var appName = CompileConstants.IsDebug ? "DEVELOPMENT USE ONLY" : "Stardew Valley Preview";

        AppVersion = GetAppVersion();
        AppName = $"{appName}:";
    }

    private static string GetAppVersion()
    {
        return CompileConstants.IsDebug ? $"Debug build - {ApplicationConstants.CommitHash}" : ApplicationConstants.Version.ToString();
    }
}
