using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Imaging;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls.Alerts;
using NexusMods.App.UI.Extensions;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;
using SkiaSharp;

namespace NexusMods.App.UI.Controls.MiniGameWidget;

public partial class MiniGameWidget : ReactiveUserControl<IMiniGameWidgetViewModel>
{
    public MiniGameWidget()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.Image, v => v.GameImage.Source)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Name, v => v.NameTextBlock.Text)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.IsFound, v => v.IsFoundTextBlock.IsVisible)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel,
                        vm => vm.IsFound,
                        v => v.NotFoundTextBlock.IsVisible,
                        isFound => !isFound
                    )
                    .DisposeWith(d);

                this.WhenAnyValue(view => view.ViewModel!.GameInstallations)
                    .Subscribe(installations =>
                        {
                            if(installations is null)
                                return;
                            
                            var tooltip = string.Join(", ", installations.Select(installation => installation.Store.ToString()));
                            ToolTip.SetTip(IsFoundTextBlock, tooltip);
                        }
                    )
                    .DisposeWith(d);
            }
        );
    }
}
