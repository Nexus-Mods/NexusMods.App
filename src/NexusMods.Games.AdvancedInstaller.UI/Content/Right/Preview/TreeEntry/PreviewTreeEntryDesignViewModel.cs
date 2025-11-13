
using NexusMods.Sdk.Games;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

public class PreviewTreeEntryDesignViewModel() : PreviewTreeEntryViewModel(
    new GamePath(LocationId.Game, "Data/Textures/texture.dds"),
    false,
    true);
