using Avalonia.ReactiveUI;
using ReactiveUI;
namespace NexusMods.App.UI.Overlays.ManageGameWarning;

/// <summary>
/// This is a variant of an 'OkCancel' messagebox with some small customizations that prevent
/// subclassing of the 'OkCancel' messagebox (e.g. runs of bold, no cross, etc.)
///
/// 'If you have existing mods, they will be detected and can be used alongside the app in the ‘External Changes’ page.'
/// ...
/// </summary>
public partial class ManageGameWarningView : ReactiveUserControl<IManageGameWarningViewModel>
{
    public ManageGameWarningView()
    {
        InitializeComponent();
        
        ViewForMixins.WhenActivated(this, _ =>
        {
            OkButton.Command = ReactiveCommand.Create(() =>
            {
                ViewModel!.Complete(true);
            });

            CancelButton.Command = ReactiveCommand.Create(() =>
            {
                ViewModel!.Complete(false);
            });
        });
    }
}

