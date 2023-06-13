using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays;

public partial class NexusLoginOverlayView : ReactiveUserControl<INexusLoginOverlayViewModel>
{
    public NexusLoginOverlayView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            CopyButton.Command = ReactiveCommand.CreateFromTask(async () =>
            {
                await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(ViewModel!.Uri.ToString());
            });

            this.BindCommand(ViewModel, vm => vm.Cancel, v => v.CancelButton)
                .DisposeWith(d);
            this.WhenAnyValue(view => view.ViewModel!.Uri)
                .BindTo(this, view => view.UrlTextBlock.Text)
                .DisposeWith(d);

        });
    }
}

