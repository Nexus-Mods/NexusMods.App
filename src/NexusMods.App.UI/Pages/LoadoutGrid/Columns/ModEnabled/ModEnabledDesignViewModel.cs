﻿using System.Reactive;
using System.Windows.Input;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;

public class ModEnabledDesignViewModel : AViewModel<IModEnabledViewModel>, IModEnabledViewModel
{
    public ModId Row { get; set; } = Initializers.ModId;

    [Reactive]
    public bool Enabled { get; set; }

    [Reactive] public ModStatus Status { get; set; } = ModStatus.Failed;
    public ReactiveCommand<bool, Unit> ToggleEnabledCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteModCommand { get; }

    public ModEnabledDesignViewModel()
    {
        ToggleEnabledCommand = ReactiveCommand.CreateFromTask<bool, Unit>(async state =>
        {
            // Bit of a delay to show simulate the roundtrip to the datastore
            await Task.Delay(2000);
            Enabled = state;
            return Unit.Default;
        });

        DeleteModCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            Status = ModStatus.Installing;
            await Task.Delay(2000);
            Status = ModStatus.Installed;
        });
    }
}
