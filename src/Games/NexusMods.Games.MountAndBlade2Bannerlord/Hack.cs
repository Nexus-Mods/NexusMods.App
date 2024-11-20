using System.Collections;
using Bannerlord.ModuleManager;
namespace NexusMods.Games.MountAndBlade2Bannerlord;

/// <summary>
/// Temporary code, to make game 'usable' while we wait for approval to make the game
/// properly usable. Scheduled for DELETION.
/// </summary>
public static class Hack
{
    private static ModuleInfoExtended MakeDummyOfficialModule(string id)
    {
        return new ModuleInfoExtended(id, id, true,
            new ApplicationVersion(ApplicationVersionType.Release, 1, 2,
                1, 1
            ), true, false, [],
            [], [], [],
            [], ""
        );
    }
    
    public static IEnumerable<ModuleInfoExtended> GetDummyBaseGameModules()
    {
        // These are ordered as they are in launcher defaults.
        // Do not reorder, I didn't fully stub the items above
        yield return MakeDummyOfficialModule("Native");
        yield return MakeDummyOfficialModule("SandBoxCore");
        yield return MakeDummyOfficialModule("CustomBattle");
        yield return MakeDummyOfficialModule("Sandbox");
        yield return MakeDummyOfficialModule("StoryMode");
        yield return MakeDummyOfficialModule("BirthAndDeath");
    }
}
