using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using NexusMods.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Updater;

public class UpdaterDesignViewModel : AViewModel<IUpdaterViewModel>, IUpdaterViewModel
{
    [Reactive]
    public InstallationMethod Method { get; set; } = InstallationMethod.Archive;

    [Reactive]
    public Version NewVersion { get; set; } = Version.Parse("0.2.3");

    [Reactive]
    public Version OldVersion { get; set; } = Version.Parse("0.1.0");


    public ICommand LaterCommand { get; }
    public ICommand UpdateCommand { get; }
    public ICommand ShowChangelog { get;}

    [Reactive]
    public bool ShowSystemUpdateMessage { get; set; }

    public async Task<bool> MaybeShow()
    {
        return true;
    }

    [Reactive]
    public bool ShouldShow { get; set; } = true;

    public UpdaterDesignViewModel()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Method)
                .Select(m => m is not (InstallationMethod.Archive or InstallationMethod.InnoSetup))
                .BindTo(this, view => view.ShowSystemUpdateMessage)
                .DisposeWith(d);
        });

        LaterCommand = ReactiveCommand.Create(() =>
        {
            IsActive = false;
        });

        UpdateCommand = ReactiveCommand.Create(() =>
        {
            IsActive = false;
            UpdateClicked = true;
        });

        ShowChangelog = ReactiveCommand.Create(() =>
        {
            ChangelogShown = true;
        });
    }

    [Reactive]
    public bool UpdateClicked { get; set; }

    [Reactive]
    public bool ChangelogShown { get; set; }

    [Reactive]
    public bool IsActive { get; set; }
}
