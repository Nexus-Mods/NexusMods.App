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
using NexusMods.Icons;
using ReactiveUI;
using SkiaSharp;

namespace NexusMods.App.UI.Controls.MiniGameWidget;

public partial class MiniGameWidget : ReactiveUserControl<IMiniGameWidgetViewModel>
{
    private StackPanel? _gameStack = null;
    private StackPanel? _placeholderStack = null;
    
    public MiniGameWidget()
    {
        InitializeComponent();
        
        
        
        this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.Image, v => v.GameImage.Source)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Name, v => v.NameTextBlock.Text)
                    .DisposeWith(d);
            }
        );
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        _gameStack = e.NameScope.Find<StackPanel>("GameStackPanel");
        _placeholderStack = e.NameScope.Find<StackPanel>("PlaceholderStackPanel");
        
        Console.WriteLine(ViewModel);
        
        if (_gameStack == null || _placeholderStack == null)
            return;
        
        //_gameStack.IsVisible = !ViewModel.Placeholder;
        //_placeholderStack.IsVisible = ViewModel.Placeholder;
    }
}
