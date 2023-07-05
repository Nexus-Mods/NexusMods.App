using System.Text;
using System.Text.Json;
using NexusMods.Games.StardewValley.Analyzers;
using NexusMods.Paths;

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

    public static Dictionary<RelativePath, byte[]> CreateTestFiles(SMAPIManifest manifest)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest);

        return new Dictionary<RelativePath, byte[]>
        {
            { "manifest.json", bytes }
        };
    }
}
