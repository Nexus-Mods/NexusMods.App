using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace NexusMods.UI.Theme.Controls;

public partial class SpineGame : UserControl
{
    public static readonly StyledProperty<IImage> SourceProperty = Image.SourceProperty.AddOwner<SpineGame>();
    
    public IImage Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
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