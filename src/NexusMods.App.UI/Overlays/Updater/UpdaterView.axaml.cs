using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using ReactiveUI;
using NexusMods.App.UI.Resources;

namespace NexusMods.App.UI.Overlays.Updater;

public partial class UpdaterView : ReactiveUserControl<IUpdaterViewModel>
{
    public UpdaterView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {

            this.WhenAnyValue(view => view.ViewModel!.NewVersion)
                .Select(v => $"{Language.Updater_UpdateAvailable}: v{v}")
                .BindToUi(this, view => view.UpdateHeadingTextBlock.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.ShowSystemUpdateMessage)
                .Select(show => !show)
                .BindToUi(this, view => view.UpdateButton.IsEnabled)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.ShowSystemUpdateMessage)
                .BindToUi(this, view => view.UseSystemUpdater.IsVisible)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.ShowUninstallInstructionsCommand)
                .BindToUi(this, view => view.ViewUninstallDocsButton.Command)
                .DisposeWith(d);
            
            this.WhenAnyValue(view => view.ViewModel!.UpdateCommand)
                .BindToUi(this, view => view.UpdateButton.Command)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.LaterCommand)
                .BindToUi(this, view => view.LaterButton.Command)
                .DisposeWith(d);
        });
    }
}

