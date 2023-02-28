using System.Buffers;

namespace NexusMods.DataModel;

public static class StreamExtensions
{
    /// <summary>
    /// Writes the given lines to the stream
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="lines"></param>
    /// <param name="token"></param>
    public static async Task WriteAllLinesAsync(this Stream stream, IEnumerable<string> lines, string separator = "\r\n", CancellationToken token = default)
    {
        await using var writer = new StreamWriter(stream, leaveOpen: true);
        foreach (var line in lines)
        {
            await writer.WriteAsync(line.AsMemory(), token);
            await writer.WriteAsync(separator);
        }
    }
    
    public static async Task<string> ReadAllTextAsync(this Stream stream, CancellationToken token = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        return await reader.ReadToEndAsync(token);
    }
}