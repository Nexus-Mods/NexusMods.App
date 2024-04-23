using FluentAssertions;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.BethesdaGameStudios.SkyrimSpecialEdition;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.FOMOD.Tests;

public class FomodXmlInstallerTests : AModInstallerTest<SkyrimSpecialEdition, FomodXmlInstaller>
{
    public FomodXmlInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    private async Task<IEnumerable<ModInstallerResult>> GetResultsFromDirectory(string testCase)
    {
        var relativePath = $"TestCasesPacked/{testCase}.fomod";
        var fullPath = FileSystem.GetKnownPath(KnownPath.EntryDirectory)
            .Combine(relativePath);
        var downloadId = await FileOriginRegistry.RegisterDownload(fullPath);

        var analysis = FileOriginRegistry.Get(downloadId);
        var installer = FomodXmlInstaller.Create(ServiceProvider, new GamePath(LocationId.Game, ""));
        var tree = TreeCreator.Create(analysis.Contents, FileStore);

        var install = GameInstallation;
        var info = new ModInstallerInfo()
        {
            ArchiveFiles = tree,
            BaseModId = ModId.NewId(),
            Locations = install.LocationsRegister,
            GameName = install.Game.Name,
            Store = install.Store,
            Version = install.Version,
            ModName = "",
            Source = analysis,
        };
        return await installer.GetModsAsync(info);
    }

    [Fact]
    public async Task PriorityHighIfScriptExists()
    {
        var results = await GetResultsFromDirectory("SimpleInstaller");
        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task InstallsFilesSimple()
    {
        var results = await GetResultsFromDirectory("SimpleInstaller");
        results.SelectMany(f => f.Files)
            .Should()
            .AllBeAssignableTo<IToFile>()
            .Which
            .Should()
            .HaveCount(2)
            .And.Satisfy(
                x => x.To.FileName == "g1p1f1.out.esp",
                x => x.To.FileName == "g2p1f1.out.esp"
            );
    }

    [Fact]
    public async Task InstallsFilesComplex_WithImages()
    {
        var results = await GetResultsFromDirectory("WithImages");
        results.SelectMany(f => f.Files)
            .Should()
            .AllBeAssignableTo<IToFile>()
            .Which
            .Should()
            .HaveCount(3)
            .And.Satisfy(
                // In group 1, the second plugin is recommended
                x => x.To.FileName == "g1p2f1.out.esp",
                // In group 2, both plugins are required
                x => x.To.FileName == "g2p1f1.out.esp",
                x => x.To.FileName == "g2p2f1.out.esp"
            );
    }

    [Fact]
    public async Task InstallsFilesComplex_WithMissingImage()
    {
        var results = await GetResultsFromDirectory("WithMissingImage");
        results.SelectMany(f => f.Files)
            .Should()
            .AllBeAssignableTo<IToFile>()
            .Which
            .Should().HaveCount(3)
            .And.Satisfy(
                // In group 1, the second plugin is recommended
                x => x.To.FileName == "g1p2f1.out.esp",
                // In group 2, both plugins are required
                x => x.To.FileName == "g2p1f1.out.esp",
                x => x.To.FileName == "g2p2f1.out.esp"
            );
    }

    [Fact]
    public async Task InstallsFilesSimple_UsingRar()
    {
        var results = await GetResultsFromDirectory("SimpleInstaller-rar");
        results.SelectMany(f => f.Files)
            .Should()
            .AllBeAssignableTo<IToFile>()
            .Which
            .Should().HaveCount(2)
            .And.Satisfy(
                x => x.To.FileName == "g1p1f1.out.esp",
                x => x.To.FileName == "g2p1f1.out.esp"
            );
    }

    [Fact]
    public async Task InstallsFilesSimple_Using7z()
    {
        var results = await GetResultsFromDirectory("SimpleInstaller-7z");
        results.SelectMany(f => f.Files)
            .Should()
            .AllBeAssignableTo<IToFile>()
            .Which
            .Should().HaveCount(2)
            .And.Satisfy(
                x => x.To.FileName == "g1p1f1.out.esp",
                x => x.To.FileName == "g2p1f1.out.esp"
            );
    }

    [Fact]
    public async Task InstallFilesNestedWithImages()
    {
        var results = await GetResultsFromDirectory("NestedWithImages.zip");
        results.SelectMany(f => f.Files)
            .Should()
            .AllBeAssignableTo<IToFile>()
            .Which
            .Should()
            .HaveCount(3)
            .And.Satisfy(
                // In group 1, the second plugin is recommended
                x => x.To.FileName == "g1p2f1.out.esp",
                // In group 2, both plugins are required
                x => x.To.FileName == "g2p1f1.out.esp",
                x => x.To.FileName == "g2p2f1.out.esp"
            );
    }

    [Fact]
    public async Task InstallFilesMultipleNestedWithImages()
    {
        var results = await GetResultsFromDirectory("MultipleNestingWithImages.7z");
        results.SelectMany(f => f.Files)
            .Should()
            .AllBeAssignableTo<IToFile>()
            .Which
            .Should()
            .HaveCount(3)
            .And.Satisfy(
                // In group 1, the second plugin is recommended
                x => x.To.FileName == "g1p2f1.out.esp",
                // In group 2, both plugins are required
                x => x.To.FileName == "g2p1f1.out.esp",
                x => x.To.FileName == "g2p2f1.out.esp"
            );
    }

    [Fact]
    public async Task ObeysTypeDescriptors()
    {
        var results = await GetResultsFromDirectory("ComplexInstaller");
        results.SelectMany(f => f.Files)
            .Should()
            .AllBeAssignableTo<IToFile>()
            .Which
            .Should().HaveCount(3)
            .And.Satisfy(
                // In group 1, the second plugin is recommended
                x => x.To.FileName == "g1p2f1.out.esp",
                // In group 2, both plugins are required
                x => x.To.FileName == "g2p1f1.out.esp",
                x => x.To.FileName == "g2p2f1.out.esp"
            );
    }

    [Fact]
    public async Task ResilientToCaseInconsistencies()
    {
        var results = await GetResultsFromDirectory("ComplexInstallerCaseChanges.7z");
        results.SelectMany(f => f.Files)
            .Should()
            .AllBeAssignableTo<IToFile>()
            .Which
            .Should().HaveCount(3)
            .And.Satisfy(
                // In group 1, the second plugin is recommended
                x => x.To.FileName.Equals("g1p2f1.out.esp"),
                // In group 2, both plugins are required
                x => x.To.FileName.Equals("g2p1f1.out.esp"),
                x => x.To.FileName.Equals("g2p2f1.out.esp")
            );
    }


    #region Tests for Broken FOMODs. Don't install them, don't throw. Only log. No-Op

    [Fact]
    public async Task Broken_WithEmptyGroup()
    {
        var results = await GetResultsFromDirectory("Broken-EmptyGroup");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Broken_WithEmptyOption()
    {
        var results = await GetResultsFromDirectory("Broken-EmptyOption");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Broken_WithEmptyStep()
    {
        var results = await GetResultsFromDirectory("Broken-EmptyStep");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Broken_WithoutSteps()
    {
        var results = await GetResultsFromDirectory("Broken-NoSteps");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Broken_WithoutModuleName()
    {
        var results = await GetResultsFromDirectory("Broken-NoModuleName");
        results.Should().BeEmpty();
    }

    #endregion

}
