namespace NexusMods.Stores.Steam;

public class Program
{
    public static async Task<int> Main(string[] argv)
    {
        var client = new Session();
        await client.ConnectAsync();

        return 0;
    }
}
