using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Sdk.IO;

namespace NexusMods.Sdk.FileStore;

/// <summary>
/// A helper class for <see cref="IFileStore"/> that represents a file to be backed up. The Path is optional,
/// but should be provided if it is expected that the paths will be used for extraction or mod installation.
/// </summary>
/// <param name="StreamFactory"></param>
/// <param name="Hash"></param>
/// <param name="Size"></param>
public readonly record struct ArchivedFileEntry(IStreamFactory StreamFactory, Hash Hash, Size Size);
