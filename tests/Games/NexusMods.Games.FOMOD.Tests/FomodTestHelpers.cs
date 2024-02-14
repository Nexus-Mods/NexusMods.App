using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Extensions.BCL;
using NexusMods.Extensions.Hashing;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees;
using FileSystem = NexusMods.Paths.FileSystem;

namespace NexusMods.Games.FOMOD.Tests;

/// <summary>
/// Helper methods for the tests.
/// </summary>
public static class FomodTestHelpers
{
    /// <summary>
    /// Gets a file tree for the specified test case, this is often used to feed into the FOMOD analyzer.
    /// </summary>
    /// <param name="testCase"></param>
    /// <returns></returns>
    public static async ValueTask<KeyedBox<RelativePath, ModFileTree>> GetFomodTree(string testCase)
    {
        var relativePath = $"TestCases/{testCase}".ToRelativePath();
        var baseFolder = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine(relativePath);

        var entries = await baseFolder
            .EnumerateFileEntries()
            .SelectAsync(async entry => new ModFileTreeSource()
            {
                Path = entry.Path.RelativeTo(baseFolder),
                Hash = (await entry.Path.XxHash64Async()).Value,
                Size = entry.Size.Value,
                Factory = new NativeFileStreamFactory(entry.Path)
            })
            .ToArrayAsync();

        return ModFileTree.Create(entries);
    }
}
