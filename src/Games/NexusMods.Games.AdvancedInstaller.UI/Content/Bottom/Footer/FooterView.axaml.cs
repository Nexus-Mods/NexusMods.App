using System.Diagnostics.CodeAnalysis;
using Avalonia.ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;

[ExcludeFromCodeCoverage]
public partial class FooterView : ReactiveUserControl<IFooterViewModel>
{
    public FooterView()
    {
        InitializeComponent();
    }
}
