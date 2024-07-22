using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Trees.Traits;
using Xunit.DependencyInjection;

namespace NexusMods.Games.RedEngine.Tests.LibraryArchiveInstallerTests;

public class SimpleOverlayInstallerTests : ALibraryArchiveInstallerTests<SimpleOverlayModInstaller>
{
    public SimpleOverlayInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
    
    /// <summary>
    /// Test cases, key is the name, the values are the archive file paths.
    /// </summary>
    public static readonly Dictionary<string, string[]> TestCases = new()
    {
        { "Files Under No Folder", ["bin/x64/foo.exe", "archive/pc/mod/foo.archive"] },
        { "Files Under Sub Folders", ["mymod/bin/x64/foo.exe", "mymod/archive/pc/mod/foo.archive"] },
        { "All Common Prefixes", ["bin/x64/foo.exe", "engine/foo.exe", "r6/foo.exe", "red4ext/foo.exe", "archive/pc/mod/foo.archive"] },
    };

    public static IEnumerable<object[]> TestCaseData()
    {
        foreach (var (key, value) in TestCases)
        {
            yield return [key, value];
        }
    }

    
    [Theory]
    [MethodData(nameof(TestCaseData))]
    public async Task FilesAreMappedToCorrectFolders(string testCaseName, string[] archivePaths)
    {
        var loadout = await CreateLoadout();
        var archive = await AddFromPaths(archivePaths);
        var result = await Install(loadout, archive);
        
        archive.Children.Count.Should().Be(archivePaths.Length, "The number of files should match the number of files in the archive.");
        
        if (archive.GetTree().GetFiles().Length != archive.Children.Count)
            throw new InvalidOperationException("The number of files should match the number of files in the archive.");
        
        result.Length.Should().Be(1, "The installer should have installed one group of files.");
        
        if (!result[0].TryGetAsLoadoutItemGroup(out var group))
            throw new InvalidOperationException("The result should be a LoadoutItemGroup.");
        if (group.Children.Count != archivePaths.Length)
            throw new InvalidOperationException("The number of files should match the number of files in the archive.");
        group.Children.Count.Should().Be(archivePaths.Length, "The number of files should match the number of files in the archive.");
        
        await VerifyChildren(ChildrenFilesAndHashes(result[0]), archivePaths).UseParameters(testCaseName);
    }

    private SettingsTask VerifyChildren(IEnumerable<(RelativePath FromPath, Hash Hash, GamePath GamePath)> childrenFilesAndHashes, string[] archivePaths)
    {
        var asArray = childrenFilesAndHashes.ToArray();
        asArray.Length.Should().Be(archivePaths.Length, "The number of files should match the number of files in the archive.");
        
        return Verify(asArray.Select(row =>
                new
                {
                    FromPath = row.FromPath.ToString(),
                    Hash = row.Hash.ToString(),
                    ToGamePath = row.GamePath.ToString(),
                }
        )); 
    }
}
