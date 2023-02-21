using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace NexusMods.UI.Theme.Controls;

public partial class SpineHome : UserControl
{
    public static readonly DirectProperty<SpineHome, bool?> IsCheckedProperty = AvaloniaProperty.RegisterDirect<SpineHome, bool?>(nameof(IsChecked),
        x => x._toggle?.IsChecked, (x, v) => x._toggle!.IsChecked = v, unsetValue: false, defaultBindingMode: BindingMode.TwoWay);

    private readonly ToggleButton? _toggle;

    public bool? IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }
    
    public SpineHome()
    {
        AffectsRender<SpineHome>(IsCheckedProperty);
        InitializeComponent();
        _toggle = this.Find<ToggleButton>("ToggleButton");
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}