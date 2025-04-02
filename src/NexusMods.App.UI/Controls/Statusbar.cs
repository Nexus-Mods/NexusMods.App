using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using NexusMods.Icons;

namespace NexusMods.App.UI.Controls;

public class Statusbar : ItemsControl
{
    private static readonly FuncTemplate<Panel?> DefaultPanel =
        new(() => new StackPanel() { Orientation = Orientation.Horizontal, Spacing = 8 });

    static Statusbar()
    {
        ItemsPanelProperty.OverrideDefaultValue<Statusbar>(DefaultPanel);
    }
}
