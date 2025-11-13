using System.Collections.Frozen;
using System.Xml;
using FluentAssertions;
using FomodInstaller.Interface;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.Sdk.Library;
using NexusMods.Sdk.Games;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.Games.FOMOD.Tests;

public class FomodXmlInstallerTests(ITestOutputHelper outputHelper) : ALibraryArchiveInstallerTests<FomodXmlInstallerTests, Cyberpunk2077Game>(outputHelper)
{
    
    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        ConfigOptions.RegisterNullGuidedInstaller = false;
        return base.AddServices(services)
            .AddSingleton<ICoreDelegates, MockDelegates>()
            .AddRedEngineGames()
            .AddUniversalGameLocator<Cyberpunk2077Game>(new Version("1.6.1"));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("/", "")]
    [InlineData("/foo", "foo")]
    [InlineData("\\", "")]
    [InlineData("\\foo", "foo")]
    [InlineData("foo", "foo")]
    [InlineData("foo/bar", "foo/bar")]
    public void Test_RemoveRoot(string? input, string expected)
    {
        var actual = FomodXmlInstaller.RemoveRoot(input).ToString();
        actual.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(TestData_FixPath))]
    public void Test_FixPath(string? input, string expected, bool isDirectory, FrozenDictionary<RelativePath, LibraryArchiveFileEntry.ReadOnly> archiveFiles)
    {
        var actual = FomodXmlInstaller.FixPath(input, archiveFiles, isDirectory: isDirectory, logger: Logger);
        actual.Should().Be(expected);
    }

    public static TheoryData<string?, string, bool, FrozenDictionary<RelativePath, LibraryArchiveFileEntry.ReadOnly>> TestData_FixPath()
    {
        return new TheoryData<string?, string, bool, FrozenDictionary<RelativePath, LibraryArchiveFileEntry.ReadOnly>>
        {
            { "", "", false, FrozenDictionary<RelativePath, LibraryArchiveFileEntry.ReadOnly>.Empty },
            { "/", "", false, FrozenDictionary<RelativePath, LibraryArchiveFileEntry.ReadOnly>.Empty },
            { "/foo\\bar.txt", "foo/bar.txt", false, new Dictionary<RelativePath, LibraryArchiveFileEntry.ReadOnly>
            {
                { "foo/bar.txt", default },
            }.ToFrozenDictionary() },
        };
    }

    private async Task<LoadoutItemGroup.ReadOnly> GetResultsFromDirectory(string testCase)
    {
        var relativePath = $"TestCasesPacked/{testCase}.fomod";
        var fullPath = FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine(relativePath);

        var archive = await RegisterLocalArchive(fullPath);
        var installer = FomodXmlInstaller.Create(ServiceProvider, new GamePath(LocationId.Game, ""));

        var loadout = await CreateLoadout();

        var group = await Install(installer, loadout, archive);
        return group;
    }

    [Fact]
    public async Task PriorityHighIfScriptExists()
    {
        var results = await GetResultsFromDirectory("SimpleInstaller");
        results.Children.Should().NotBeEmpty();
    }

    [Fact]
    public async Task InstallsFilesSimple()
    {
        var results = await GetResultsFromDirectory("SimpleInstaller");
        results.Children.Should().HaveCount(2).And.Satisfy(
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g1p1f1.out.esp",
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p1f1.out.esp"
        );
    }

    [Fact]
    public async Task InstallsFilesComplex_WithImages()
    {
        var results = await GetResultsFromDirectory("WithImages");
        results.Children.Should().HaveCount(3).And.Satisfy(
            // In group 1, the second plugin is recommended
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g1p2f1.out.esp",
            // In group 2, both plugins are required
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p1f1.out.esp",
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p2f1.out.esp"
        );
    }


    [Fact]
    public async Task InstallsFilesComplex_WithMissingImage()
    {
        var results = await GetResultsFromDirectory("WithMissingImage");
        results.Children.Should().HaveCount(3).And.Satisfy(
            // In group 1, the second plugin is recommended
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g1p2f1.out.esp",
            // In group 2, both plugins are required
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p1f1.out.esp",
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p2f1.out.esp"
        );
    }

    [Fact]
    public async Task InstallsFilesSimple_UsingRar()
    {
        var results = await GetResultsFromDirectory("SimpleInstaller-rar");
        results.Children.Should().HaveCount(2).And.Satisfy(
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g1p1f1.out.esp",
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p1f1.out.esp"
        );
    }

    [Fact]
    public async Task InstallsFilesSimple_Using7z()
    {
        var results = await GetResultsFromDirectory("SimpleInstaller-7z");
        results.Children.Should().HaveCount(2).And.Satisfy(
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g1p1f1.out.esp",
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p1f1.out.esp"
        );
    }

    [Fact]
    public async Task InstallFilesNestedWithImages()
    {
        var results = await GetResultsFromDirectory("NestedWithImages.zip");
        results.Children.Should().HaveCount(3).And.Satisfy(
            // In group 1, the second plugin is recommended
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g1p2f1.out.esp",
            // In group 2, both plugins are required
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p1f1.out.esp",
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p2f1.out.esp"
        );
    }

    [Fact]
    public async Task InstallFilesMultipleNestedWithImages()
    {
        var results = await GetResultsFromDirectory("MultipleNestingWithImages.7z");
        results.Children.Should().HaveCount(3).And.Satisfy(
            // In group 1, the second plugin is recommended
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g1p2f1.out.esp",
            // In group 2, both plugins are required
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p1f1.out.esp",
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p2f1.out.esp"
        );
    }

    [Fact]
    public async Task ObeysTypeDescriptors()
    {
        var results = await GetResultsFromDirectory("ComplexInstaller");
        results.Children.Should().HaveCount(3).And.Satisfy(
            // In group 1, the second plugin is recommended
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g1p2f1.out.esp",
            // In group 2, both plugins are required
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p1f1.out.esp",
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName == "g2p2f1.out.esp"
        );
    }

    [Fact]
    public async Task ResilientToCaseInconsistencies()
    {
        var results = await GetResultsFromDirectory("ComplexInstallerCaseChanges.7z");
        results.Children.Should().HaveCount(3).And.Satisfy(
            // In group 1, the second plugin is recommended
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName.Equals("g1p2f1.out.esp"),
            // In group 2, both plugins are required
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName.Equals("g2p1f1.out.esp"),
            x => ((GamePath)x.ToLoadoutItemWithTargetPath().TargetPath).FileName.Equals("g2p2f1.out.esp")
        );
    }


    #region Tests for Broken FOMODs. Don't install them, don't throw. Only log. No-Op

    [Fact]
    public async Task Broken_WithEmptyGroup()
    {
        var act = async () => await GetResultsFromDirectory("Broken-EmptyGroup");
        await act.Should().ThrowAsync<XmlException>();
    }

    [Fact]
    public async Task Broken_WithEmptyOption()
    {
        var act = async () => await GetResultsFromDirectory("Broken-EmptyOption");
        await act.Should().ThrowAsync<XmlException>();
    }

    [Fact]
    public async Task Broken_WithEmptyStep()
    {
        var act = async () => await GetResultsFromDirectory("Broken-EmptyStep");
        await act.Should().ThrowAsync<XmlException>();
    }

    [Fact]
    public async Task Broken_WithoutSteps()
    {
        var act = async () => await GetResultsFromDirectory("Broken-NoSteps");
        await act.Should().ThrowAsync<XmlException>();
    }

    [Fact]
    public async Task Broken_WithoutModuleName()
    {
        var act = async () => await GetResultsFromDirectory("Broken-NoModuleName");
        await act.Should().ThrowAsync<XmlException>();
    }

    #endregion

}
