using System.Diagnostics.CodeAnalysis;
using Avalonia.ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.EmptyPreview;

[ExcludeFromCodeCoverage]
public partial class EmptyPreviewView : ReactiveUserControl<IEmptyPreviewViewModel>
{
    public EmptyPreviewView()
    {
        InitializeComponent();
    }
}
