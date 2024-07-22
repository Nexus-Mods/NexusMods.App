using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using Xunit.DependencyInjection;

namespace NexusMods.Games.RedEngine.Tests.LibraryArchiveInstallerTests;

public class PathBasedInstallerTests : ALibraryArchiveInstallerTests
{
    public PathBasedInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
    
    /// <summary>
    /// Test cases, key is the name, the values are the archive file paths.
    /// </summary>
    public static readonly List<(string TestName, Type InstallerType, string[] Paths)> TestCases = new()
    {
        ( "Files Under No Folder", typeof(SimpleOverlayModInstaller), ["bin/x64/foo.exe", "archive/pc/mod/foo.archive"] ),
        ( "Files Under Sub Folders", typeof(SimpleOverlayModInstaller), ["mymod/bin/x64/foo.exe", "mymod/archive/pc/mod/foo.archive"] ),
        ( "All Common Prefixes", typeof(SimpleOverlayModInstaller), ["bin/x64/foo.exe", "engine/foo.exe", "r6/foo.exe", "red4ext/foo.exe", "archive/pc/mod/foo.archive"] ),
        ( "Files with no folder", typeof(FolderlessModInstaller), ["folder/filea.archive", "fileb.archive"] ),
    };

    public static IEnumerable<object[]> TestCaseData()
    {
        foreach (var row in TestCases)
        {
            yield return [row.TestName, row.InstallerType, row.Paths];
        }
    }

    
    [Theory]
    [MethodData(nameof(TestCaseData))]
    public async Task FilesAreMappedToCorrectFolders(string testCaseName, Type installerType, string[] archivePaths)
    {
        var loadout = await CreateLoadout();
        var archive = await AddFromPaths(archivePaths);
        var result = await Install(installerType, loadout, archive);
        
        result.Length.Should().Be(1, "The installer should have installed one group of files.");
        
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
