using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Trees;

namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;

[UsedImplicitly]
public class ModFilesViewModel : AViewModel<IModFilesViewModel>, IModFilesViewModel
{
    private readonly ILoadoutRegistry _registry;
    public IFileTreeViewModel? FileTreeViewModel { get; private set; }
    
    public ModFilesViewModel(ILoadoutRegistry registry)
    {
        _registry = registry;
    }

    public void Initialize(LoadoutId loadoutId, ModId modId)
    {
        FileTreeViewModel = new ModFileTreeViewModel(loadoutId, modId, _registry);
    }

}
