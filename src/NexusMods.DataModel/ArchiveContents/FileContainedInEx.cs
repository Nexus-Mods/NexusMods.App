using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveContents;

[JsonName("FileContainedInEx")]
public record FileContainedInEx : Entity
{
    public override EntityCategory Category => EntityCategory.FileContainedInEx;
    public RelativePath File { get; init; }
}
