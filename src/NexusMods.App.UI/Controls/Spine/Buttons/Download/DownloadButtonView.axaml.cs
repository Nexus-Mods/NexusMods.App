using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

public partial class DownloadButtonView : ReactiveUserControl<IDownloadButtonViewModel>
{
    public DownloadButtonView()
    {
        InitializeComponent();
    }
}

