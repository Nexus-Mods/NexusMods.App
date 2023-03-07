using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace NexusMods.App.UI.Controls.Buttons.RoundedButton;

public partial class RoundedButton : ReactiveUserControl<IRoundedButtonViewModel>
{
    public RoundedButton()
    {
        InitializeComponent();
    }
}

