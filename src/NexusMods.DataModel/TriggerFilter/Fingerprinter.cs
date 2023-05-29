using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.TriggerFilter;

public struct Fingerprinter
{
    private BinaryWriter _binaryWriter;

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
}
