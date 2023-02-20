using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace NexusMods.UI.Theme.Controls;

public partial class SpineGame : UserControl
{
    public static readonly StyledProperty<IImage> SourceProperty = AvaloniaProperty.Register<SpineGame, IImage>(nameof(Source));
    
    public static readonly DirectProperty<SpineGame, bool> IsCheckedProperty = AvaloniaProperty.RegisterDirect<SpineGame, bool>(nameof(IsChecked),
        x => x.IsChecked, (x, v) => x.IsChecked = v, unsetValue: false, defaultBindingMode: BindingMode.TwoWay);
    
    public IImage Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }
    
    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public SpineGame()
    {
        InitializeComponent();
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}