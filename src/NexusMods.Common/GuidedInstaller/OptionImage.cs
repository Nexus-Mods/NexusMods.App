using NexusMods.Hashing.xxHash64;
using OneOf;

namespace NexusMods.Common.GuidedInstaller;

/// <summary>
/// An image.
/// </summary>
public sealed class OptionImage : OneOfBase<Uri, OptionImage.ImageFromArchive>
{
    /// <summary>
    /// Represents an image from an archive.
    /// </summary>
    public record struct ImageFromArchive(Hash FileHash);

    /// <inheritdoc />
    public OptionImage(OneOf<Uri, ImageFromArchive> input) : base(input) { }
}
