using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Shared;

public class AGameTestContext
{
    public static AGameTestContext Create(
        Func<IDictionary<RelativePath, byte[]>, Task<TemporaryPath>> createTestArchive,
        Func<Loadout.Model, AbsolutePath, string?, CancellationToken, Task<Mod.Model>> installModStoredFileIntoLoadout) =>
        new(createTestArchive, installModStoredFileIntoLoadout);

    public Func<IDictionary<RelativePath, byte[]>, Task<TemporaryPath>> CreateTestArchive { get; }
    public Func<Loadout.Model, AbsolutePath, string?, CancellationToken, Task<Mod.Model>> InstallModStoredFileIntoLoadout { get; }

    private AGameTestContext(Func<IDictionary<RelativePath, byte[]>, Task<TemporaryPath>> createTestArchive, Func<Loadout.Model, AbsolutePath, string?, CancellationToken, Task<Mod.Model>> installModStoredFileIntoLoadout)
    {
        CreateTestArchive = createTestArchive;
        InstallModStoredFileIntoLoadout = installModStoredFileIntoLoadout;
    }
}
