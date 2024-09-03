using Avalonia;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;
using NexusMods.App.UI.Resources;
using R3;
using ReactiveUI;
namespace NexusMods.App.UI.Overlays.Dependency.MissingProtontricks;

public partial class MissingProtontricksView : ReactiveUserControl<IMissingProtontricksViewModel>
{
    public MissingProtontricksView()
    {
        InitializeComponent();

        this.WhenActivated(_ =>
        {
            OkButton.Command = ReactiveCommand.Create(() =>
            {
                ViewModel!.Complete(Unit.Default);
            });

            CloseButton.Command = ReactiveCommand.Create(() =>
            {
                ViewModel!.Complete(Unit.Default);
            });
        });
    }
}

