using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// Interface used to wrap all file specific metadata tied to a file which belongs to a mod,
/// such as dependency information.<br/><br/>
///
/// Instances of this interface are produced by <see cref="IFileMetadataSource"/>.<br/><br/>
///
/// This interface defines no contract. It essentially acts as a named <see cref="object"/>
/// for tracking/code base readability purposes.
/// </summary>
public interface IModFileMetadata
{
}
