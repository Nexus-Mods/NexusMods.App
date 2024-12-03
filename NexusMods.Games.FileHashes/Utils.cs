using System.IO.Hashing;
using NexusMods.Games.FileHashes.DTO;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Games.FileHashes;

public static class Utils
{
    public static async Task<GameFileHashes> HashFile(Stream stream, RelativePath path)
    {
        var xxHash3 = new XxHash3();
        
        stream.Position = 0;
        var buffer = GC.AllocateUninitializedArray<byte>(1024 * 16);
        while (true)
        {
            var read = await stream.ReadAsync(buffer);
            
            if (read == 0)
                break;
            var readSpan = buffer.AsSpan(0, read);
            xxHash3.Append(readSpan);
        }
        
        
        return new GameFileHashes
        {
            Path = path,
            XxHash3 = Hash.From(xxHash3.GetCurrentHashAsUInt64()),
            MinimalHash = await stream.MinimalHash(),
        };
    }
    
}
