using NexusMods.Common;
using NexusMods.DataModel.ModInstallers;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;
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
    public static async ValueTask<FileTreeNode<RelativePath, ModSourceFileEntry>> GetFomodTree(string testCase)
    {
        var relativePath = $"TestCases/{testCase}".ToRelativePath();
        var baseFolder = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
            .Combine(relativePath);
        var entries = baseFolder
            .EnumerateFileEntries()
            .SelectAsync(async entry => KeyValuePair.Create(entry.Path.RelativeTo(baseFolder),
                new ModSourceFileEntry
                {
                    Size = entry.Size,
                    Hash = await entry.Path.XxHash64Async(),
                    StreamFactory = new NativeFileStreamFactory(entry.Path)
                }))
            .ToArrayAsync();
        return FileTreeNode<RelativePath, ModSourceFileEntry>.CreateTree(await entries);
    }
}
