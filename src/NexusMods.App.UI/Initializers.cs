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
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.LeftMenu.Home;
using ReactiveUI;

// ReSharper disable InconsistentNaming

namespace NexusMods.App.UI;

public static class Initializers
{
    public static readonly ICommand ICommand = ReactiveCommand.Create(() => { });
    public static IImage IImage => new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Opaque);

    public static readonly ReactiveCommand<Unit, Unit> EnabledReactiveCommand = ReactiveCommand.Create(() => { }, Observable.Return(true));
    public static readonly ReactiveCommand<Unit, Unit> DisabledReactiveCommand = ReactiveCommand.Create(() => { }, Observable.Return(false));

    public static ReactiveCommand<TInput, Unit> CreateReactiveCommand<TInput>(bool disabled = true)
    {
        return ReactiveCommand.Create<TInput, Unit>(_ => Unit.Default, Observable.Return(disabled));
    }

    public static ReactiveCommand<TInput, TOutput> CreateReactiveCommand<TInput, TOutput>()
    {
        return ReactiveCommand.Create<TInput, TOutput>(_ => throw new UnreachableException(), Observable.Return(false));
    }

    public static readonly ModCursor ModCursor = new(LoadoutId,
        ModId.From(new Guid("00000000-0000-0000-0000-000000000002")));

    public static LoadoutId LoadoutId =
        LoadoutId.From(new Guid("00000000-0000-0000-0000-000000000001"));

    public static ReadOnlyObservableCollection<T> ReadOnlyObservableCollection<T>()
    {
        return new ReadOnlyObservableCollection<T>(new ObservableCollection<T>());
    }
}
