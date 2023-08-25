using System.Diagnostics;
using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DevelopmentBuildBanner;

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
        this.WhenActivated(_ => Text = $"{appVersion} - DEVELOPMENT USE ONLY");
    }

    private static string GetAppVersion()
    {
        #if DEBUG
            return "Debug build";
        #endif
        return $"v{Process.GetCurrentProcess().MainModule!.FileVersionInfo.ProductVersion}";
    }
}
