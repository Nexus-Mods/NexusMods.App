using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public class SuggestedEntryDesignViewModel() : SuggestedEntryViewModel(
    Guid.NewGuid(),
    CreateDesignAbsolutePath(),
    LocationId.From("Data"),
    new GamePath(LocationId.Game, "Data"))
{
    private static AbsolutePath CreateDesignAbsolutePath()
    {
        var fs = new InMemoryFileSystem();
        return fs.FromUnsanitizedFullPath("C:/Games/Skyrim Special Edition/Data");
    }
}
