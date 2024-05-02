using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

// ReSharper disable InconsistentNaming

namespace NexusMods.App.UI;

public static class Initializers
{
    public static readonly ICommand ICommand = ReactiveCommand.Create(() => { });
    public static IImage IImage => new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Opaque);

    public static readonly ReactiveCommand<Unit, Unit> EmptyReactiveCommand = ReactiveCommand.Create(() => { });
    public static readonly ReactiveCommand<Unit, Unit> EnabledReactiveCommand = ReactiveCommand.Create(() => { }, Observable.Return(true));
    public static readonly ReactiveCommand<Unit, Unit> DisabledReactiveCommand = ReactiveCommand.Create(() => { }, Observable.Return(false));

    public static readonly LoadoutId LoadoutId = LoadoutId.From(EntityId.From(0xDEADBEEF));
    public static readonly ModId ModId = ModId.From(EntityId.From(0xDEADBEAF));
}
