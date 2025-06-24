using NexusMods.Paths;

namespace NexusMods.Sdk.IO;

/// <summary>
/// Represents a Stream Factory backed by a native path on the FileSystem.
/// </summary>
public class NativeFileStreamFactory : IStreamFactory
{
    /// <summary/>
    /// <param name="file">Absolute path of the file.</param>
    public NativeFileStreamFactory(AbsolutePath file) => Path = file;

    /// <inheritdoc/>
    public RelativePath FileName => Path.Name;

    /// <summary>
    /// Absolute path to the file.
    /// </summary>
    public AbsolutePath Path { get; }

    /// <inheritdoc />
    public ValueTask<Stream> GetStreamAsync()
    {
        return new ValueTask<Stream>(Path.Open(FileMode.Open, FileAccess.Read, FileShare.Read));
    }
}
