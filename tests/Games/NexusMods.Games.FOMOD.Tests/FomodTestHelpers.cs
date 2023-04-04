using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.FOMOD.Tests;

/// <summary>
/// Helper methods for the tests.
/// </summary>
public static class FomodTestHelpers
{
    /// <summary>
    /// Gets the path to the .fomod for a specified test case.
    /// </summary>
    /// <param name="testCase">Name of one of the folders under the 'TestCasesPacked' folder.</param>
    /// <returns>Path to the .fomod.</returns>
    public static AbsolutePath GetFomodPath(string testCase)
    {
        var entry = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory);
        var relativePath = $"TestCasesPacked/{testCase}.fomod".ToRelativePath();
        return entry.CombineUnchecked(relativePath);
    }

    /// <summary>
    /// Gets the path to the FOMOD XML for a specified test case.
    /// </summary>
    /// <param name="testCase">Name of one of the folders under the 'TestCases' folder.</param>
    /// <returns>Path to the module config.</returns>
    public static AbsolutePath GetXmlPath(string testCase)
    {
        var entry = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory);
        var relativePath = $"TestCases/{testCase}/{FomodConstants.XmlConfigRelativePath}".ToRelativePath();
        return entry.CombineUnchecked(relativePath);
    }

    /// <summary>
    /// Gets the path to the FOMOD XML for a specified test case.
    /// </summary>
    /// <param name="testCase">Name of one of the folders under the 'TestCases' folder.</param>
    /// <returns>Path to the module config and stream to the underlying file.</returns>
    public static async Task<(AbsolutePath path, MemoryStream stream)> GetXmlPathAndStreamAsync(string testCase)
    {
        var path = GetXmlPath(testCase);
        return (path, new MemoryStream(await FileSystem.Shared.ReadAllBytesAsync(path)));
    }
}
