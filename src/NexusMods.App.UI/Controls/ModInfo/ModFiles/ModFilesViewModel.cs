using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;

[UsedImplicitly]
public class ModFilesViewModel(IConnection conn) : AViewModel<IModFilesViewModel>, IModFilesViewModel
{
    public IFileTreeViewModel? FileTreeViewModel { get; private set; }
    
    public void Initialize(LoadoutId loadoutId, ModId modId)
    {
        FileTreeViewModel = new ModFileTreeViewModel(loadoutId, modId, conn);
    }

}
