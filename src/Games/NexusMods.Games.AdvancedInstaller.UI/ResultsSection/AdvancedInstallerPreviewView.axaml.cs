using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public partial class AdvancedInstallerPreviewView : ReactiveUserControl<IAdvancedInstallerPreviewViewModel>
{
    public AdvancedInstallerPreviewView()
    {
        InitializeComponent();
    }
}

