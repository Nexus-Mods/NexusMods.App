using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.CLI.Tests.VerbTests;
using NexusMods.DataModel.Loadouts.Extensions;
using NexusMods.DataModel.LoadoutSynchronizer.Extensions;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using Noggog;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Games.BethesdaGameStudios.Tests.SkyrimSpecialEditionTests;

public class SkyrimSpecialEditionTests : AGameTest<SkyrimSpecialEdition.SkyrimSpecialEdition>
{
    private readonly TestModDownloader _downloader;
    private AVerbTest _verbTester;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    public SkyrimSpecialEditionTests(TestModDownloader downloader, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _downloader = downloader;
        _verbTester = new AVerbTest(serviceProvider);
    }

    [Fact]
    public void CanFindGames()
    {
        Game.Name.Should().Be("Skyrim Special Edition");
        Game.Domain.Should().Be(SkyrimSpecialEdition.SkyrimSpecialEdition.StaticDomain);
        GameRegistry.Installations.Values.Where(g => g.Game == Game).Should().NotBeEmpty();
    }

    [Fact]
    [Trait("FlakeyTest", "True")]
    public async Task CanInstallAndApplyMostPopularMods()
    {
        const int skseModId = 30379;
        const int skseFileId = 323365;
        const string skseModName = "skse64";

        const int skyuiModId = 12604;
        const int skyuiFileId = 35407;
        const string skyuiModName = "skyui";

        const int ussepModId = 266;
        const int ussepFileId = 392477;
        const string ussepModName = "Unofficial skyrim special edition patch";

        // manage the game
        // Note: can't create the loadout using CLI as it would index the game files,
        // and other tests might pollute the game folder in the meantime.
        var loadout = await CreateLoadout(indexGameFiles: false);
        var loadoutName = loadout.Name;

        var modPath = FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Assets/TruncatedPlugins.7z");
        await InstallModStoredFileIntoLoadout(loadout, modPath, "Skyrim Truncated Plugins");

        var log = await _verbTester.Run("list-loadouts");

        log.LastTableColumns.Should().BeEquivalentTo("Name", "Game", "Id", "Mod Count");
        log.TableCellsWith(loadoutName).Should().NotBeEmpty();

        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(3);

        // install skse
        var uri = $"nxm://{Game.Domain}/mods/{skseModId}/files/{skseFileId}";
        log = await _verbTester.Run("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n", skseModName);

        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(4);

        log = await _verbTester.Run("list-mod-contents", "-l", loadoutName, "-m", skseModName);
        log.LastTable.Rows.Count().Should().Be(128);

        // install skyui
        uri = $"nxm://{Game.Domain}/mods/{skyuiModId}/files/{skyuiFileId}";
        log = await _verbTester.Run("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n",
            skyuiModName);

        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(5);

        log = await _verbTester.Run("list-mod-contents", "-l", loadoutName, "-m", skyuiModName);
        log.LastTable.Rows.Count().Should().Be(6);

        // install ussep
        uri = $"nxm://{Game.Domain}/mods/{ussepModId}/files/{ussepFileId}";
        log = await _verbTester.Run("download-and-install-mod", "-u", uri, "-l", loadoutName, "-n",
            ussepModName);

        log = await _verbTester.Run("list-mods", "-l", loadoutName);
        log.LastTable.Rows.Count().Should().Be(6);

        log = await _verbTester.Run("list-mod-contents", "-l", loadoutName, "-m", ussepModName);
        log.LastTable.Rows.Count().Should().Be(8);

        // Test Apply
        log = await _verbTester.Run("flatten-loadout", "-l", loadoutName);
        // count plugins.txt
        var logger = ServiceProvider.GetRequiredService<ILogger<SkyrimSpecialEditionTests>>();
        StringBuilder sb = new();
        log.LastTable.Rows.ForEach(r =>
        {
            r.ForEach(c => sb.Append(c.ToString() + ","));
            sb.AppendLine();
        });
        logger.LogInformation("flatten-list table {FlattenTable}", sb.ToString());
        log.LastTable.Rows.Count().Should().Be(223);

        log = await _verbTester.Run("apply", "-l", loadoutName);
    }

    [Fact]
    public async Task CanGeneratePluginsFile()
    {
        var loadout = await CreateLoadout(indexGameFiles: false);
        var mod = await InstallTruncatedPlugins(loadout);

        Refresh(ref loadout);
        var analysisStr = await BethesdaTestHelpers.GetAssetsPath(FileSystem).Combine("plugin_dependencies.json")
            .ReadAllTextAsync();
        var analysis = JsonSerializer.Deserialize<Dictionary<string, string[]>>(analysisStr)!;


        var metadataFiles =
            loadout.Mods.First(m => m.Category == ModCategory.Metadata); // <= throws on failure

        var gameFiles =
            loadout.Mods.First(m => m.Category == ModCategory.GameFiles); // <= throws on failure

        var modPath = FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Assets/SMIM_Truncated_Plugins.7z");
        await InstallModStoredFileIntoLoadout(loadout, modPath, "SMIM");

        var file = metadataFiles.Files.First(f => f.TryGetAsGeneratedFile(out _));
        file.TryGetAsGeneratedFile(out var pluginOrderFile);
        var generator = pluginOrderFile!.Generator;
        
        var flattened = await loadout.ToFlattenedLoadout();

        await Task.Delay(100);
        using var ms = new MemoryStream();
        await generator.Write(pluginOrderFile, ms, loadout, flattened, await loadout.ToFileTree());
        await ms.FlushAsync();

        await Verify(Encoding.UTF8.GetString(ms.ToArray()));
    }

    /// <summary>
    /// Installs the test mod that contains around 80 truncated plugins. These are the game plugins
    /// and a few mods, but they have all been stripped of everything but their headers. So this is purely
    /// the metadata of the plugins, which is all we need to test the plugin order file generation.
    /// </summary>
    /// <param name="loadout"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<Mod.Model> InstallTruncatedPlugins(Loadout.Model loadout)
    {
        var path = FileSystem.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("Assets/TruncatedPlugins.7z");
        var mod = await InstallModStoredFileIntoLoadout(loadout, path, "TruncatedPlugins");
        return mod;
    }

    [Fact]
    public async Task EnablingAndDisablingModsModifiesThePluginsFile()
    {
        var loadout = await CreateLoadout(indexGameFiles: false);

        loadout.Mods.SelectMany(m => m.Files)
            .Where(t => t.To.FileName == "plugin_test.esp")
            .Should()
            .BeEmpty("the mod is not installed");

        PluginOrderFile? pluginOrderFile = null;
        GeneratedFile.Model? file = null;
        foreach (var f in loadout.Files)
        {
            if (!f.TryGetAsGeneratedFile(out var generator) || generator.Generator is not PluginOrderFile pof) continue;
            file = generator;
            pluginOrderFile = pof;
            break;
        }
        
        file.Should().NotBeNull("the plugin order file should exist in the loadout");


        var pluginFilePath = file!.To.CombineChecked(loadout.Installation);

        var path = BethesdaTestHelpers.GetDownloadableModFolder(FileSystem, "SkyrimBase");
        var downloaded = await _downloader.DownloadFromManifestAsync(path, FileSystem);

        var skyrimBase = await InstallModStoredFileIntoLoadout(
            loadout,
            downloaded.Path,
            downloaded.Manifest.Name);

        Refresh(ref loadout);
        await loadout.Apply();
        Refresh(ref loadout);

        pluginFilePath.FileExists.Should().BeTrue("the loadout is applied");


        var text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().NotContain("plugin_test.esp", "plugin_test.esp is not installed");

        path = BethesdaTestHelpers.GetDownloadableModFolder(FileSystem, "PluginTest");
        downloaded = await _downloader.DownloadFromManifestAsync(path, FileSystem);
        var pluginTest = await InstallModStoredFileIntoLoadout(
            loadout,
            downloaded.Path,
            downloaded.Manifest.Name);

        Refresh(ref loadout);
        await loadout.Apply();
        Refresh(ref loadout);

        pluginFilePath.FileExists.Should().BeTrue("the loadout is applied");
        text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().Contain("plugin_test.esp", "plugin_test.esp is installed");

        // Disable the mod
        await pluginTest.ToggleEnabled();
        Refresh(ref loadout);

        text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().Contain("plugin_test.esp", "new loadout has not been applied yet");

        await loadout.Apply();

        text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().NotContain("plugin_test.esp", "plugin_test.esp is disabled");

        // Enable the mod
        await pluginTest.ToggleEnabled();
        Refresh(ref loadout);
        await loadout.Apply();
        Refresh(ref loadout);

        text = await GetPluginOrder(pluginFilePath);

        text.Should().Contain("Skyrim.esm");
        text.Should().Contain("plugin_test.esp", "plugin_test.esp is enabled again");

    }

    private static async Task<string[]> GetPluginOrder(AbsolutePath pluginFilePath)
    {
        return (await pluginFilePath.ReadAllTextAsync())
            .Split(["\r", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.TrimStart("*"))
            .ToArray();
    }
}
