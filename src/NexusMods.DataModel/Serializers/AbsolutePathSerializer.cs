using System.Buffers;
using System.Text;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel.Serializers;

internal class AbsolutePathSerializer(IFileSystem fileSystem) : IValueSerializer<AbsolutePath>
{
    private static readonly Encoding Encoding = Encoding.UTF8;
    
    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        // Not the fastest comparison, but it's fairly rare
        var aParsed = Read(a);
        var bParsed = Read(b);
        
        var cmp = string.Compare(aParsed.Directory, bParsed.Directory, StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
            return cmp;
        return string.Compare(aParsed.FileName, bParsed.FileName, StringComparison.OrdinalIgnoreCase);
    }

    public Type NativeType => typeof(AbsolutePath);
    public Symbol UniqueId => Symbol.Intern<AbsolutePathSerializer>();
    
    public AbsolutePath Read(ReadOnlySpan<byte> buffer)
    {
        var str = Encoding.GetString(buffer);
        return fileSystem.FromUnsanitizedFullPath(str);
    }

    public void Serialize<TWriter>(AbsolutePath value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var str = value.ToString();
        var size = Encoding.GetByteCount(str);
        var span = buffer.GetSpan(size);
        Encoding.GetBytes(str, span);
        buffer.Advance(size);
    }
}
