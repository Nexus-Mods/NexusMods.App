using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace NexusMods.UI.Theme.Controls;

public partial class SpineAdd : UserControl
{
    public static readonly DirectProperty<SpineAdd, bool?> IsCheckedProperty = AvaloniaProperty.RegisterDirect<SpineAdd, bool?>(nameof(IsChecked),
        x => x._toggle?.IsChecked, (x, v) => x._toggle!.IsChecked = v, unsetValue: false, defaultBindingMode: BindingMode.TwoWay);

    private readonly ToggleButton? _toggle;

    public bool? IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }
    
    public SpineAdd()    {
        AffectsRender<SpineAdd>(IsCheckedProperty);
        InitializeComponent();
        _toggle = this.Find<ToggleButton>("ToggleButton");
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}