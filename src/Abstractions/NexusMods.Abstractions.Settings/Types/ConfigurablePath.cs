using JetBrains.Annotations;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Represents a configurable path. This is made up of a <see cref="KnownPath"/>
/// base directory and a <see cref="RelativePath"/> file part.
/// </summary>
[PublicAPI]
public readonly struct ConfigurablePath
{
    /// <summary>
    /// The base directory part.
    /// </summary>
    public readonly KnownPath BaseDirectory;

    /// <summary>
    /// The file part.
    /// </summary>
    public readonly RelativePath File;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ConfigurablePath(KnownPath baseDirectory, RelativePath file)
    {
        BaseDirectory = baseDirectory;
        File = file;
    }

    /// <summary>
    /// Converts to a <see cref="AbsolutePath"/>.
    /// </summary>
    public AbsolutePath ToPath(IFileSystem fileSystem)
    {
        if (BaseDirectory == default(KnownPath) && File == default(RelativePath))
            throw new InvalidOperationException($"This {nameof(ConfigurablePath)} contains only default values!");

        return fileSystem.GetKnownPath(BaseDirectory).Combine(File);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{BaseDirectory}/{File}";
    }
}
