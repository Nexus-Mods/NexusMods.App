using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.TopBar;

public partial class TopBarView : ReactiveUserControl<TopBarViewModel>
{
    public TopBarView()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, x => x.Title, x => x.Login.Content)
                .DisposeWith(d);
        });
    }
}