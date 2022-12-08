using NexusMods.DataModel.ModLists.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModLists.ApplySteps;

public record CopyFile : IApplyStep, IStaticFileStep
{
    public required AbsolutePath To { get; init; }
    public required AStaticModFile From { get; init; }
    public Hash Hash => From.Hash;
    public Size Size => From.Size;
}