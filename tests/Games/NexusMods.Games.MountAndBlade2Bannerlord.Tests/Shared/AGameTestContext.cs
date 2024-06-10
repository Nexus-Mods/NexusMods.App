using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Shared;

public class AGameTestContext
{
    public static AGameTestContext Create(
        Func<IDictionary<RelativePath, byte[]>, Task<TemporaryPath>> createTestArchive,
        Func<Loadout.ReadOnly, AbsolutePath, string?, CancellationToken, Task<Mod.ReadOnly>> installModStoredFileIntoLoadout) =>
        new(createTestArchive, installModStoredFileIntoLoadout);

    public Func<IDictionary<RelativePath, byte[]>, Task<TemporaryPath>> CreateTestArchive { get; }
    public Func<Loadout.ReadOnly, AbsolutePath, string?, CancellationToken, Task<Mod.ReadOnly>> InstallModStoredFileIntoLoadout { get; }

    private AGameTestContext(Func<IDictionary<RelativePath, byte[]>, Task<TemporaryPath>> createTestArchive, Func<Loadout.ReadOnly, AbsolutePath, string?, CancellationToken, Task<Mod.ReadOnly>> installModStoredFileIntoLoadout)
    {
        CreateTestArchive = createTestArchive;
        InstallModStoredFileIntoLoadout = installModStoredFileIntoLoadout;
    }
}
