using System.Reactive.Disposables;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using DotNetBrowser.Browser;
using ReactiveUI;

namespace NexusMods.App.UI.WebBrowser;

public partial class WebBrowserPageView : ReactiveUserControl<IWebBrowserPageViewModel>
{
    private readonly IBrowser _browser;

    public WebBrowserPageView()
    {
        _browser = WebEngineProvider.GetBrowser();
        _browser.Navigation.LoadUrl("https://www.nexusmods.com");
        
        InitializeComponent();


        BrowserControl.InitializeFrom(_browser);

        
        this.WhenActivated(d =>
            {
                this.WhenAnyValue(v => v.ViewModel!.Address)
                    .Subscribe(uri =>
                    {
                        _browser.Navigation.LoadUrl(uri.ToString());
                    })
                    .DisposeWith(d);
            }
        );
        
        _browser.Navigation.NavigationStarted += (_, args) =>
        {
            ViewModel!.NavigatedTo(new Uri(args.Url));
        };
    }

    private void DoIt_OnClick(object? sender, RoutedEventArgs e)
    {
    }
}

