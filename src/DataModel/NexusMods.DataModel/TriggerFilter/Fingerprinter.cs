using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.TriggerFilter;

/// <summary>
/// A helper class for generating fingerprints, which are essentially content hashes
/// </summary>
public struct Fingerprinter : IDisposable
{
    private BinaryWriter _binaryWriter;

    /// <summary>
    /// Creates a new fingerprinter instance
    /// </summary>
    /// <returns></returns>
    public static Fingerprinter Create()
    {
        return new Fingerprinter
        {
            _binaryWriter = new BinaryWriter(new MemoryStream())
        };
    }
    
    /// <summary>
    /// Adds a string to the fingerprinter
    /// </summary>
    /// <param name="value"></param>
    public void Add(string value)
    {
        _binaryWriter.Write(value);
    }

    /// <summary>
    /// Adds an id to the fingerprinter
    /// </summary>
    /// <param name="id"></param>
    public void Add(IId id)
    {
        Span<byte> bytes = stackalloc byte[id.SpanSize + 1];
        id.ToTaggedSpan(bytes);
        _binaryWriter.Write(bytes);
    }
    
    /// <summary>
    /// Finalizes the fingerprinter and returns the hash
    /// </summary>
    /// <returns></returns>
    public Hash Digest()
    {
        _binaryWriter.Flush();
        var ms = ((MemoryStream) _binaryWriter.BaseStream);
        ms.Position = 0;
        XxHash64Algorithm algo = new();
        
        var hash = Hash.FromULong(algo.HashBytes(ms.ToArray()));
        
        _binaryWriter.Close();
        return hash;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _binaryWriter.Dispose();
    }
}
