using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace NexusMods.UI.Theme.Controls.Spine.Buttons;

public partial class Game : UserControl
{
    public static readonly StyledProperty<IImage> SourceProperty = AvaloniaProperty.Register<Game, IImage>(nameof(Source));
    
    public static readonly DirectProperty<Game, bool?> IsCheckedProperty = AvaloniaProperty.RegisterDirect<Game, bool?>(nameof(IsChecked),
        x => x._toggle?.IsChecked, (x, v) => x._toggle!.IsChecked = v, unsetValue: false, defaultBindingMode: BindingMode.TwoWay);

    private readonly ToggleButton? _toggle;

    public IImage Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public bool? IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }
    
    public Game()
    {
        AffectsRender<Home>(IsCheckedProperty, SourceProperty);
        InitializeComponent();
        _toggle = this.Find<ToggleButton>("ToggleButton");
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}