using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Trees;

namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;

public interface IModFilesViewModel : IViewModelInterface
{
    IFileTreeViewModel? FileTreeViewModel { get; }
    void Initialize(LoadoutId loadoutId, ModId modId) { }
}
