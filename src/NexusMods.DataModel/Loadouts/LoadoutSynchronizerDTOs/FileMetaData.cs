using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;

/// <summary>
/// A helper class to store the metadata of a file
/// </summary>
/// <param name="Path"></param>
/// <param name="Hash"></param>
/// <param name="Size"></param>
public record FileMetaData(AbsolutePath Path, Hash Hash, Size Size);