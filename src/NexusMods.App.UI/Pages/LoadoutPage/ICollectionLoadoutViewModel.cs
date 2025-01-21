using Avalonia.Media.Imaging;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.WorkspaceSystem;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public interface ICollectionLoadoutViewModel : IPageViewModelInterface
{
    LoadoutTreeDataGridAdapter Adapter { get; }

    bool IsLocalCollection { get; }

    bool IsReadOnly { get; }

    bool IsCollectionEnabled { get; }

    string Name { get; }

    RevisionNumber RevisionNumber { get; }

    string AuthorName { get; }

    Bitmap? AuthorAvatar { get; }

    Bitmap? TileImage { get; }

    Bitmap? BackgroundImage { get; }

    ReactiveCommand<Unit> CommandToggle { get; }
}
