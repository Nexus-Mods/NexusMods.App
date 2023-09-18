using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Updater;

public partial class UpdaterView : ReactiveUserControl<IUpdaterViewModel>
{
    public UpdaterView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.OldVersion)
                .Select(v => $"v{v}")
                .BindToUi(this, view => view.FromVersionTextBlock.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.NewVersion)
                .Select(v => $"v{v}")
                .BindToUi(this, view => view.ToVersionTextBlock.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.ShowSystemUpdateMessage)
                .Select(show => !show)
                .BindToUi(this, view => view.UpdateButton.IsEnabled)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.ShowSystemUpdateMessage)
                .BindToUi(this, view => view.UseSystemUpdater.IsVisible)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.ShowChangelog)
                .BindToUi(this, view => view.ChangelogButton.Command)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.UpdateCommand)
                .BindToUi(this, view => view.UpdateButton.Command)
                .DisposeWith(d);

        });
    }
}

