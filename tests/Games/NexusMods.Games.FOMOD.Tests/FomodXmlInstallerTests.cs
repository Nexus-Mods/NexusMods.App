using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using Xunit.Sdk;

namespace NexusMods.Games.FOMOD.Tests;

public class FomodXmlInstallerTests : ALibraryArchiveInstallerTests<Cyberpunk2077Game>
{
    public FomodXmlInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    private async Task<LoadoutItem.ReadOnly[]> GetResultsFromDirectory(string testCase)
    {
        var relativePath = $"TestCasesPacked/{testCase}.fomod";
        var fullPath = FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine(relativePath);

        var archive = await RegisterLocalArchive(fullPath);
        var installer = FomodXmlInstaller.Create(ServiceProvider, new GamePath(LocationId.Game, ""));

        var loadout = await CreateLoadout();

        var res = await Install(installer, loadout, archive);
        return res;
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
        results.Should().HaveCount(2).And.Satisfy(
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g1p1f1.out.esp",
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p1f1.out.esp"
        );
    }

    [Fact]
    public async Task InstallsFilesComplex_WithImages()
    {
        var results = await GetResultsFromDirectory("WithImages");
        results.Should().HaveCount(3).And.Satisfy(
            // In group 1, the second plugin is recommended
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g1p2f1.out.esp",
            // In group 2, both plugins are required
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p1f1.out.esp",
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p2f1.out.esp"
        );
    }


    [Fact]
    public async Task InstallsFilesComplex_WithMissingImage()
    {
        var results = await GetResultsFromDirectory("WithMissingImage");
        results.Should().HaveCount(3).And.Satisfy(
            // In group 1, the second plugin is recommended
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g1p2f1.out.esp",
            // In group 2, both plugins are required
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p1f1.out.esp",
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p2f1.out.esp"
        );
    }

    [Fact]
    public async Task InstallsFilesSimple_UsingRar()
    {
        var results = await GetResultsFromDirectory("SimpleInstaller-rar");
        results.Should().HaveCount(2).And.Satisfy(
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g1p1f1.out.esp",
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p1f1.out.esp"
        );
    }

    [Fact]
    public async Task InstallsFilesSimple_Using7z()
    {
        var results = await GetResultsFromDirectory("SimpleInstaller-7z");
        results.Should().HaveCount(2).And.Satisfy(
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g1p1f1.out.esp",
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p1f1.out.esp"
        );
    }

    [Fact]
    public async Task InstallFilesNestedWithImages()
    {
        var results = await GetResultsFromDirectory("NestedWithImages.zip");
        results.Should().HaveCount(3).And.Satisfy(
            // In group 1, the second plugin is recommended
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g1p2f1.out.esp",
            // In group 2, both plugins are required
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p1f1.out.esp",
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p2f1.out.esp"
        );
    }

    [Fact]
    public async Task InstallFilesMultipleNestedWithImages()
    {
        var results = await GetResultsFromDirectory("MultipleNestingWithImages.7z");
        results.Should().HaveCount(3).And.Satisfy(
            // In group 1, the second plugin is recommended
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g1p2f1.out.esp",
            // In group 2, both plugins are required
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p1f1.out.esp",
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p2f1.out.esp"
        );
    }

    [Fact]
    public async Task ObeysTypeDescriptors()
    {
        var results = await GetResultsFromDirectory("ComplexInstaller");
        results.Should().HaveCount(3).And.Satisfy(
            // In group 1, the second plugin is recommended
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g1p2f1.out.esp",
            // In group 2, both plugins are required
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p1f1.out.esp",
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName == "g2p2f1.out.esp"
        );
    }

    [Fact]
    public async Task ResilientToCaseInconsistencies()
    {
        var results = await GetResultsFromDirectory("ComplexInstallerCaseChanges.7z");
        results.Should().HaveCount(3).And.Satisfy(
            // In group 1, the second plugin is recommended
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName.Equals("g1p2f1.out.esp"),
            // In group 2, both plugins are required
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName.Equals("g2p1f1.out.esp"),
            x => x.ToLoadoutItemWithTargetPath().TargetPath.FileName.Equals("g2p2f1.out.esp")
        );
    }


    #region Tests for Broken FOMODs. Don't install them, don't throw. Only log. No-Op

    [Fact]
    public async Task Broken_WithEmptyGroup()
    {
        var act = async () => await GetResultsFromDirectory("Broken-EmptyGroup");
        await act.Should().ThrowAsync<XunitException>();
    }

    [Fact]
    public async Task Broken_WithEmptyOption()
    {
        var act = async () => await GetResultsFromDirectory("Broken-EmptyOption");
        await act.Should().ThrowAsync<XunitException>();
    }

    [Fact]
    public async Task Broken_WithEmptyStep()
    {
        var act = async () => await GetResultsFromDirectory("Broken-EmptyStep");
        await act.Should().ThrowAsync<XunitException>();
    }

    [Fact]
    public async Task Broken_WithoutSteps()
    {
        var act = async () => await GetResultsFromDirectory("Broken-NoSteps");
        await act.Should().ThrowAsync<XunitException>();
    }

    [Fact]
    public async Task Broken_WithoutModuleName()
    {
        var act = async () => await GetResultsFromDirectory("Broken-NoModuleName");
        await act.Should().ThrowAsync<XunitException>();
    }

    #endregion

}
