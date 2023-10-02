namespace NexusMods.DataModel.LoadoutSynchronizer;

public class DiskModificationPlan
{
    /// <summary>
    /// Files that will be written to disk, via the `IToFile.Write` method.
    /// </summary>
    public required FileTree ToWrite { get; init; }

    /// <summary>
    /// Files that will be deleted from disk, via the `IToFile.Delete` method.
    /// </summary>
    public required FileTree ToDelete { get; init; }

    /// <summary>
    /// Files that will be unmodified
    /// </summary>
    public required FileTree Unmodified { get; init; }

    /// <summary>
    /// FromArchive files that will be batched to gether and extracted to disk, via the
    /// LoadoutSynchronizer.
    /// </summary>
    public required FileTree ToExtract { get; init; }
}
