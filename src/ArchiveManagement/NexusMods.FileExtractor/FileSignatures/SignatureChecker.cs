using NexusMods.Abstractions.FileExtractor;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.FileSignatures;

/// <summary>
/// Utility for checking the file types of specific files by matching the magic signatures on their headers.
/// </summary>
public class SignatureChecker : ISignatureChecker
{
    private readonly (FileType, byte[])[] _signatures;
    private readonly byte[] _buffer;

    private static readonly Dictionary<Extension, FileType> Extensions =
        Definitions.Extensions.ToDictionary(x => x.Item2, x => x.Item1);

    /// <summary>
    /// Creates a signature checker responsible for identifying file headers.
    /// </summary>
    /// <param name="types">The file types to check.</param>
    public SignatureChecker(params FileType[] types)
    {
        HashSet<FileType> types1 = new(types);
        _signatures = Definitions.Signatures.Where(row => types1.Contains(row.Item1))
            .OrderByDescending(x => x.Item2.Length).ToArray();

        _buffer = _signatures.Length > 0 ? new byte[_signatures[0].Item2.Length]
                                         : Array.Empty<byte>();
    }

    /// <inheritdoc/>
    public async ValueTask<bool> MatchesAnyAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (!stream.CanSeek) throw new ArgumentException(message: "Stream doesn't support seeking!", nameof(stream));

        var originalPos = stream.Position;
        var count = await stream.ReadAtLeastAsync(_buffer, _buffer.Length, throwOnEndOfStream: false, cancellationToken: cancellationToken);
        if (count < 1) return false;

        stream.Position = originalPos;

        foreach (var tuple in _signatures)
        {
            var (_, signature) = tuple;
            if (_buffer.AsSpan(start: 0, length: count).StartsWith(signature))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async ValueTask<IReadOnlyList<FileType>> MatchesAsync(Stream stream)
    {
        if (!stream.CanSeek) throw new ArgumentException(message: "Stream doesn't support seeking!", nameof(stream));

        var originalPos = stream.Position;
        await stream.ReadAtLeastAsync(_buffer, _buffer.Length, false);
        stream.Position = originalPos;

        var lst = new List<FileType>();
        foreach (var (fileType, signature) in _signatures)
            if (_buffer.AsSpan().StartsWith(signature))
                lst.Add(fileType);

        return lst;
    }

    /// <summary>
    /// Performs a lookup of the extension against the known extensions
    /// </summary>
    /// <param name="extension">The file extension to validate.</param>
    /// <param name="fileType">The file type used.</param>
    /// <returns>True if found, else false.</returns>
    public static bool TryGetFileType(Extension extension, out FileType fileType)
    {
        return Extensions.TryGetValue(extension, out fileType);
    }
}
