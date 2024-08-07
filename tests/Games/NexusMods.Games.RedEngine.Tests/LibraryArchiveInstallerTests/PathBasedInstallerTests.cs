using System.Runtime.CompilerServices;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using Xunit.DependencyInjection;

namespace NexusMods.Games.RedEngine.Tests.LibraryArchiveInstallerTests;

public class PathBasedInstallerTests : ALibraryArchiveInstallerTests<Cyberpunk2077Game>
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
        ( "Ignored Extensions", typeof(FolderlessModInstaller), ["folder/filea.archive", "file.txt", "docs/file.md", "bin/x64/file.pdf", "bin/x64/file.png"] ),
        ( "Appearance Preset", typeof(AppearancePresetInstaller), ["cool_choom.preset"] ),
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
        var group = await Install(installerType, loadout, archive);

        await VerifyChildren(ChildrenFilesAndHashes(group), archivePaths).UseParameters(testCaseName);
    }

    private SettingsTask VerifyChildren(IEnumerable<(RelativePath FromPath, Hash Hash, GamePath GamePath)> childrenFilesAndHashes, string[] archivePaths, [CallerFilePath] string sourceFile = "")
    {
        var asArray = childrenFilesAndHashes.ToArray();
        
        return Verify(asArray.Select(row =>
                new
                {
                    FromPath = row.FromPath.ToString(),
                    Hash = row.Hash.ToString(),
                    ToGamePath = row.GamePath.ToString(),
                }
            // ReSharper disable once ExplicitCallerInfoArgument
        ), sourceFile: sourceFile);
    }
}
