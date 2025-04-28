using NexusMods.Hashing.xxHash3;
using OneOf;

namespace NexusMods.Abstractions.GuidedInstallers;

/// <summary>
/// An image.
/// </summary>
public sealed class OptionImage : OneOfBase<Uri, OptionImage.ImageStoredFile>
{
    /// <summary>
    /// Represents an image from an archive.
    /// </summary>
    public record struct ImageStoredFile(Hash FileHash);

    /// <inheritdoc />
    public OptionImage(OneOf<Uri, ImageStoredFile> input) : base(input) { }
}
