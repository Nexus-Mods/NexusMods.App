using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

public class PreviewTreeEntryDesignViewModel : PreviewTreeEntryViewModel
{
    public PreviewTreeEntryDesignViewModel() : base(
        new GamePath(LocationId.Game, "Data/Textures/texture.dds"),
        false,
        true) { }
}
