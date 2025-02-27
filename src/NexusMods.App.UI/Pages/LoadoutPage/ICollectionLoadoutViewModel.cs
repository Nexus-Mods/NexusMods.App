using System.Reactive;
using Avalonia.Media.Imaging;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public interface ICollectionLoadoutViewModel : IPageViewModelInterface
{
    LoadoutTreeDataGridAdapter Adapter { get; }

    /// <summary>
    /// Gets whether the collection is local or remote.
    /// </summary>
    bool IsLocalCollection { get; }

    /// <summary>
    /// Gets whether the collection is read-only.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets whether the collection is enabled.
    /// </summary>
    bool IsCollectionEnabled { get; }
    
    /// <summary>
    /// Gets the number of mods installed in the collection, both required and optional.
    /// </summary>
    int InstalledModsCount { get; }

    string Name { get; }

    RevisionNumber RevisionNumber { get; }

    string AuthorName { get; }

    Bitmap? AuthorAvatar { get; }

    Bitmap? TileImage { get; }

    Bitmap? BackgroundImage { get; }

    R3.ReactiveCommand<R3.Unit> CommandToggle { get; }
    
    R3.ReactiveCommand<R3.Unit> CommandDeleteCollection { get; }
    
    ReactiveCommand<NavigationInformation, Unit> CommandViewCollectionDownloadPage { get; }
}
