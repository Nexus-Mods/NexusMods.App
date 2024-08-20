﻿using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using NexusMods.App.BuildInfo;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Updater;

public class UpdaterDesignViewModel : AOverlayViewModel<IUpdaterViewModel>, IUpdaterViewModel
{
    [Reactive]
    public InstallationMethod Method { get; set; } = InstallationMethod.Archive;

    [Reactive]
    public Version NewVersion { get; set; } = Version.Parse("0.2.3");

    [Reactive]
    public Version OldVersion { get; set; } = Version.Parse("0.1.0");


    public ICommand LaterCommand { get; }
    public ICommand UpdateCommand { get; }
    public ICommand ShowUninstallInstructionsCommand { get;}

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

        LaterCommand = ReactiveCommand.Create(Close);

        UpdateCommand = ReactiveCommand.Create(() =>
        {
            UpdateClicked = true;
            Close();
        });

        ShowUninstallInstructionsCommand = ReactiveCommand.Create(() =>
        {
            UninstallInstructionsShown = true;
        });
    }

    [Reactive]
    public bool UpdateClicked { get; set; }

    [Reactive]
    public bool UninstallInstructionsShown { get; set; }
}
