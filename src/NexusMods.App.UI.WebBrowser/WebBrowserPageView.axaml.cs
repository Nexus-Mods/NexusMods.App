using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;
using WebViewControl;

namespace NexusMods.App.UI.WebBrowser;

public partial class WebBrowserPageView : ReactiveUserControl<IWebBrowserPageViewModel>
{
    public WebBrowserPageView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
            {
                this.WhenAnyValue(v => v.ViewModel!.Address)
                    .Subscribe(uri =>
                        {
                            BrowserControl.Address = uri.ToString();
                        })
                    .DisposeWith(d);
                
                this.WhenAnyValue(v => v.BrowserControl.Address)
                    .Subscribe(uri => ViewModel!.NavigatedTo(new Uri(uri)))
                    .DisposeWith(d);
            }
        );
    }

    private void DoIt_OnClick(object? sender, RoutedEventArgs e)
    {
    }
}

