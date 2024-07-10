using NexusMods.Abstractions.Jobs;
using NexusMods.Paths;

namespace NexusMods.Library;

internal class AddLocalFileJobGroup : AJobGroup
{
    public required AbsolutePath FilePath { get; init; }
}
