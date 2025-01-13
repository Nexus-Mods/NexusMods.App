using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls.Alerts;
using NexusMods.App.UI.Controls.MiniGameWidget;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.PageHeader;

public partial class PageHeader : ReactiveUserControl<IPageHeaderViewModel>
{
    public PageHeader()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.Title, v => v.TitleTextBlock.Text)
                    .DisposeWith(d);
        
                this.OneWayBind(ViewModel, vm => vm.Description, v => v.DescriptionTextBlock.Text)
                    .DisposeWith(d);
        
                this.OneWayBind(ViewModel, vm => vm.Icon, v => v.Icon.Value)
                    .DisposeWith(d);
            }
        );

    }
}

