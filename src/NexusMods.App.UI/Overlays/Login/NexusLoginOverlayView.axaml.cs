using System.Reactive.Disposables;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Login;

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

            this.BindCommand<NexusLoginOverlayView, INexusLoginOverlayViewModel, ICommand, Button>(ViewModel, vm => vm.Cancel, v => v.CancelButton)
                .DisposeWith(d);
            this.WhenAnyValue(view => view.ViewModel!.Uri)
                .BindTo<Uri, NexusLoginOverlayView, string>(this, view => view.UrlTextBlock.Text)
                .DisposeWith(d);

        });
    }
}

