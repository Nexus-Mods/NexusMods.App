using NexusMods.Paths;

namespace NexusMods.FileExtractor.FileSignatures;

public class SignatureChecker
{
    private readonly int _maxLength;
    private readonly (FileType, byte[])[] _signatures;

    private static Dictionary<Extension, FileType> _extensions =
        Definitions.Extensions.ToDictionary(x => x.Item2, x => x.Item1);

    public SignatureChecker(params FileType[] types)
    {
        HashSet<FileType> types1 = new(types);
        _signatures = Definitions.Signatures.Where(row => types1.Contains(row.Item1))
            .OrderByDescending(x => x.Item2.Length).ToArray();
        _maxLength = _signatures.First().Item2.Length;
    }

    public async ValueTask<IReadOnlyList<FileType>> MatchesAsync(Stream stream)
    {
        var buffer = new byte[_maxLength];
        stream.Position = 0;
        await stream.ReadAsync(buffer);
        stream.Position = 0;

        var lst = new List<FileType>();
        foreach (var (fileType, signature) in _signatures)
            if (AreEqual(buffer, signature))
                lst.Add(fileType);
        return lst;
    }

    private static bool AreEqual(IReadOnlyList<byte> a, IEnumerable<byte> b)
    {
        return !b.Where((t, i) => a[i] != t).Any();
    }
    
    public bool TryGetFileType(Extension extension, out FileType fileType)
    {
        return _extensions.TryGetValue(extension, out fileType);
    }
}