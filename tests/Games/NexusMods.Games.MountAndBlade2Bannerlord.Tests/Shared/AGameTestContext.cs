using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Shared;

public class AGameTestContext
{
    public static AGameTestContext Create(
        Func<IDictionary<RelativePath, byte[]>, Task<TemporaryPath>> createTestArchive,
        Func<LoadoutMarker, AbsolutePath, string?, CancellationToken, Task<Mod>> installModStoredFileIntoLoadout) =>
        new(createTestArchive, installModStoredFileIntoLoadout);

    public Func<IDictionary<RelativePath, byte[]>, Task<TemporaryPath>> CreateTestArchive { get; }
    public Func<LoadoutMarker, AbsolutePath, string?, CancellationToken, Task<Mod>> InstallModStoredFileIntoLoadout { get; }

    private AGameTestContext(Func<IDictionary<RelativePath, byte[]>, Task<TemporaryPath>> createTestArchive, Func<LoadoutMarker, AbsolutePath, string?, CancellationToken, Task<Mod>> installModStoredFileIntoLoadout)
    {
        CreateTestArchive = createTestArchive;
        InstallModStoredFileIntoLoadout = installModStoredFileIntoLoadout;
    }
}
