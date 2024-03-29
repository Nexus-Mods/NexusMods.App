﻿using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.DataGrid;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;

public class ModEnabledViewModel : AViewModel<IModEnabledViewModel>, IModEnabledViewModel, IComparableColumn<ModCursor>
{
    private readonly ILoadoutRegistry _loadoutRegistry;

    [Reactive]
    public ModCursor Row { get; set; } = Initializers.ModCursor;

    [Reactive]
    public bool Enabled { get; set; } = false;

    [Reactive]
    public ModStatus Status { get; set; } = ModStatus.Installed;

    [Reactive]
    public ICommand ToggleEnabledCommand { get; set; }

    [Reactive]
    public ICommand DeleteModCommand { get; set; }

    public ModEnabledViewModel(ILoadoutRegistry loadoutRegistry)
    {
        _loadoutRegistry = loadoutRegistry;

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(loadoutRegistry.RevisionsAsMods)
                .Select(mod => mod.Enabled)
                .BindToUi(this, vm => vm.Enabled)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.Row)
                .SelectMany(loadoutRegistry.RevisionsAsMods)
                .Select(mod => mod.Status)
                .BindTo(this, vm => vm.Status)
                .DisposeWith(d);
        });
        ToggleEnabledCommand = ReactiveCommand.Create<bool, Unit>(enabled =>
        {
            var mod = loadoutRegistry.Get(Row);
            if (mod is null) return Unit.Default;

            var oldState = mod.Enabled ? "Enabled" : "Disabled";
            var newState = !mod.Enabled ? "Enabled" : "Disabled";

            loadoutRegistry.Alter(Row,
                $"Setting {mod.Name} from {oldState} to {newState}",
                mod =>
                {
                    if (mod?.Enabled == enabled) return mod;
                    return mod! with { Enabled = enabled };
                });
            return Unit.Default;
        });
        DeleteModCommand = ReactiveCommand.Create(() =>
        {
            var mod = loadoutRegistry.Get(Row)!;
            loadoutRegistry.Alter(Row, $"Deleting mod {mod.Name}", _ => null);
        });
    }

    public int Compare(ModCursor a, ModCursor b)
    {
        var aEnt = _loadoutRegistry.Get(a);
        var bEnt = _loadoutRegistry.Get(b);
        return (aEnt?.Enabled ?? false).CompareTo(bEnt?.Enabled ?? false);
    }
}
