namespace NexusMods.Extensions.BCL;

/// <summary/>
public static class StreamExtensions
{
    /// <summary>
    /// Writes the given lines to the stream
    /// </summary>
    /// <param name="stream">The stream to write all of the lines to.</param>
    /// <param name="lines">The lines of text to write to the stream.</param>
    /// <param name="separator">The line ending to use.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    public static async Task WriteAllLinesAsync(this Stream stream, IEnumerable<string> lines, string separator = "\r\n", CancellationToken token = default)
    {
        // TODO: It's probably faster to join all the strings first and write out once, I'll benchmark this later.
        await using var writer = new StreamWriter(stream, leaveOpen: true);
        foreach (var line in lines)
        {
            await writer.WriteAsync(line.AsMemory(), token);
            await writer.WriteAsync(separator);
        }
    }

    /// <summary>
    /// Reads all lines of text from a specified stream.
    /// </summary>
    /// <param name="stream">The stream to read the text from.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <returns>All lines of text present in the original stream.</returns>
    public static async Task<string> ReadAllTextAsync(this Stream stream, CancellationToken token = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        return await reader.ReadToEndAsync(token);
    }
}
