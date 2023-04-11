using System.Text;

namespace NexusMods.Games.StardewValley.Tests;

public static class TestHelper
{
    public static byte[] CreateManifest(out string name)
    {
        name = Guid.NewGuid().ToString("N");
        var uniqueId = Guid.NewGuid().ToString("N");

        var manifest = @$"
{{
    ""Name"": ""{name}"",
    ""Version"": ""1.0.0"",
    ""UniqueID"": ""{uniqueId}""
}}
";

        return Encoding.UTF8.GetBytes(manifest);
    }
}
