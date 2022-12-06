namespace NexusMods.IO.Async;

public static class Extensions
{
    public static async ValueTask ReadAllAsync(this Stream frm, Memory<byte> output)
    {
        var read = 0;
        while (read < output.Length)
        {
            var thisRead = await frm.ReadAsync(output[read..]);
            if (thisRead == 0)
                throw new Exception("End of stream reached before limit");
            read += thisRead;
        }
    }
}