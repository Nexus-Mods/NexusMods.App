using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.Generic.Installers;
using NexusMods.Games.Larian.BaldursGate3;
using NexusMods.Games.Larian.BaldursGate3.Installers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.Games.Larian.Tests.BaldursGate3;

public class BG3InstallerTests(ITestOutputHelper outputHelper) : ALibraryArchiveInstallerTests<BG3InstallerTests, Larian.BaldursGate3.BaldursGate3>(outputHelper)
{
    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddBaldursGate3()
            .AddUniversalGameLocator<Larian.BaldursGate3.BaldursGate3>(new Version("1.6.1"));
    }


    /// <summary>
    /// Test cases, key is the name, the values are the archive file paths.
    /// </summary>
    public static readonly List<(string TestName, Type InstallerType, string[] Paths)> TestCases =
    [
        ("BG3 Script Extender", typeof(BG3SEInstaller), ["DWrite.dll", "ScriptExtenderSettings.json"]),
        ("Simple Pak Mod", typeof(GenericPatternMatchInstaller), ["myMod.pak", "info.json"]),
        ("Nested Pak Mod", typeof(GenericPatternMatchInstaller), ["Mods/myMod.pak", "Mods/info.json", "readme.txt"]),
        ("Multiple Pak files Mod", typeof(GenericPatternMatchInstaller), ["myMod1.pak", "myMod2.pak", "info.json", "readme.txt"]),
        ("Bin Mod", typeof(GenericPatternMatchInstaller), ["bin/bink2w64.dll", "bink2w64_original.dll"]),
        ("NativeMods Mod", typeof(GenericPatternMatchInstaller), [
            "NativeMods/BG3NativeCameraTweaks.dll",
            "BG3NativeCameraTweaks.toml",
        ]),
        ("Data Mod", typeof(GenericPatternMatchInstaller), [
            "Recommended/Data/Generated/Public/Shared/Assets/Characters/_Models/Humans/_Female/_Hair/Resources/HAIR_HUM_F_Shadowheart_Spring.gr2",
            "Recommended/Data/Generated/Public/Shared/Content/Assets/Characters/Character Editor Presets/Origin Presets/[PAK]_Shadowheart/_merged.lsf",
        ]),
        ("Data Public Mod with nested Data folder", typeof(GenericPatternMatchInstaller), [
            "Public/Shared/Stats/Generated/Data/XPData1.txt",
            "Public/SharedDev/Stats/Generated/Data/XPData2.txt",
        ]),
    ];

    public static IEnumerable<object[]> TestCaseData()
    {
        return TestCases.Select(row => (object[]) [row.TestName, row.InstallerType, row.Paths]);
    }


    [Theory]
    [MemberData(nameof(TestCaseData))]
    public async Task CanInstallBG3Mods(string testCaseName, Type installerType, string[] archivePaths)
    {
        var loadout = await CreateLoadout();
        var archive = await AddFromPaths(archivePaths);
        var group = await Install(installerType, loadout, archive);

        await VerifyChildren(ChildrenFilesAndHashes(group), archivePaths).UseParameters(testCaseName);
    }


    private static SettingsTask VerifyChildren(
        IEnumerable<(RelativePath FromPath, Hashing.xxHash3.Hash Hash, Sdk.Games.GamePath GamePath)> childrenFilesAndHashes,
        string[] archivePaths,
        [CallerFilePath] string sourceFile = "")
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
            ),
            sourceFile: sourceFile
        );
    }
}
