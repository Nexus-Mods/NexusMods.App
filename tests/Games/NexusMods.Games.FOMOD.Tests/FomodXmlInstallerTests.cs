using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Games.BethesdaGameStudios;
using NexusMods.Games.TestFramework;
using NexusMods.Games.FOMOD.CoreDelegates;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.FileTree;
using NexusMods.Paths.Utilities;
using Xunit;

namespace NexusMods.Games.FOMOD.Tests;

public class FomodXmlInstallerTests : AModInstallerTest<SkyrimSpecialEdition, FomodXmlInstaller>
{
    private static Extension NoExtension = new("");
    public FomodXmlInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    private async Task<IEnumerable<ModInstallerResult>> GetResultsFromDirectory(string testCase)
    {
        var relativePath = $"TestCasesPacked/{testCase}.fomod";

        var fullPath = FileSystem.GetKnownPath(KnownPath.EntryDirectory)
            .Combine(relativePath);
        var downloadId = await DownloadRegistry.RegisterDownload(fullPath, new FilePathMetadata {
            OriginalName = fullPath.FileName,
            Quality = Quality.Low});
        var analysis = await DownloadRegistry.Get(downloadId);
        var installer = FomodXmlInstaller.Create(ServiceProvider, new GamePath(GameFolderType.Game, ""));
        var tree =
            FileTreeNode<RelativePath, ModSourceFileEntry>.CreateTree(analysis.Contents
            .Select(f => KeyValuePair.Create(
            f.Path,
            new ModSourceFileEntry
            {
                Size = f.Size,
                Hash = f.Hash,
                StreamFactory = new ArchiveManagerStreamFactory(ArchiveManager, f.Hash)
                {
                    Name = f.Path,
                    Size = f.Size
                }
            }
        )));
        return await installer.GetModsAsync(GameInstallation, ModId.New(), tree);
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
