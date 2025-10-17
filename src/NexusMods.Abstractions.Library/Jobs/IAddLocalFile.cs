using NexusMods.Abstractions.Library.Models;
using NexusMods.Paths;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Abstractions.Library.Jobs;

/// <summary>
/// A job that adds a local file to the library
/// </summary>
public interface IAddLocalFile : IJobDefinition<LocalFile.ReadOnly>
{
    /// <summary>
    /// The source file path
    /// </summary>
    public AbsolutePath FilePath { get; }
}
