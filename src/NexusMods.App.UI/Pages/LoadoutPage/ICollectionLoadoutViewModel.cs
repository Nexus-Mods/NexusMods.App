using Avalonia.Media.Imaging;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.WorkspaceSystem;
using R3;

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

    string Name { get; }

    RevisionNumber RevisionNumber { get; }

    string AuthorName { get; }

    Bitmap? AuthorAvatar { get; }

    Bitmap? TileImage { get; }

    Bitmap? BackgroundImage { get; }

    ReactiveCommand<Unit> CommandToggle { get; }
}
