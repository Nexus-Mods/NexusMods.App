using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace NexusMods.UI.Theme.Controls.Spine.Buttons;

public partial class Home : UserControl
{
    public static readonly DirectProperty<Home, bool?> IsCheckedProperty = AvaloniaProperty.RegisterDirect<Home, bool?>(nameof(IsChecked),
        x => x._toggle?.IsChecked, (x, v) => x._toggle!.IsChecked = v, unsetValue: false, defaultBindingMode: BindingMode.TwoWay);

    private readonly ToggleButton? _toggle;

    public bool? IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }
    
    public Home()
    {
        AffectsRender<Home>(IsCheckedProperty);
        InitializeComponent();
        _toggle = this.Find<ToggleButton>("ToggleButton");
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}