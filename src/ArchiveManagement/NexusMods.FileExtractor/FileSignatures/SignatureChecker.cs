using NexusMods.Paths;

namespace NexusMods.FileExtractor.FileSignatures;

/// <summary>
/// Utility for checking the file types of specific files by matching the magic signatures on their headers.
/// </summary>
public class SignatureChecker
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
        _buffer = new byte[_signatures[0].Item2.Length];
    }

    /// <summary>
    /// Checks if the header of the stream matches any known signature and returns the list of matching signatures.
    /// </summary>
    /// <param name="stream">The stream to check the header of.</param>
    /// <returns>List of matching signatures/files.</returns>
    public async ValueTask<IReadOnlyList<FileType>> MatchesAsync(Stream stream)
    {
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
