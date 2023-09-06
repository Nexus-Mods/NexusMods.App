using FluentAssertions;
using FomodInstaller.Interface;
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
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using NexusMods.Paths.Utilities;
using Xunit;

namespace NexusMods.Games.FOMOD.Tests;

public class FomodXmlInstallerTests : AModInstallerTest<SkyrimSpecialEdition, FomodXmlInstaller>
{
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

    /*

    [Fact]
    public async Task InstallsFilesComplex_WithImages()
    {
        using var testData = await SetupTestFromDirectoryAsync("WithImages");
        var installedFiles = (await testData.GetFilesToExtractAsync()).ToArray();

        installedFiles
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
        using var testData = await SetupTestFromDirectoryAsync("WithMissingImage");
        var installedFiles = (await testData.GetFilesToExtractAsync()).ToArray();

        installedFiles
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
        using var testData = await SetupTestFromDirectoryAsync("SimpleInstaller-rar");
        var installedFiles = (await testData.GetFilesToExtractAsync()).ToArray();

        installedFiles
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
        using var testData = await SetupTestFromDirectoryAsync("SimpleInstaller-7z");
        var installedFiles = (await testData.GetFilesToExtractAsync()).ToArray();

        installedFiles
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
        using var testData = await SetupTestFromDirectoryAsync("NestedWithImages.zip");
        var installedFiles = (await testData.GetFilesToExtractAsync()).ToArray();

        installedFiles
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
        using var testData = await SetupTestFromDirectoryAsync("MultipleNestingWithImages.7z");
        var installedFiles = (await testData.GetFilesToExtractAsync()).ToArray();

        installedFiles
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
        using var testData = await SetupTestFromDirectoryAsync("ComplexInstaller");
        var installedFiles = (await testData.GetFilesToExtractAsync()).ToArray();

        installedFiles
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
        using var testData = await SetupTestFromDirectoryAsync("ComplexInstallerCaseChanges.7z");
        var installedFiles = (await testData.GetFilesToExtractAsync()).ToArray();

        installedFiles
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
        using var testData = await SetupTestFromDirectoryAsync("Broken-EmptyGroup");
        var installedFiles = await testData.GetFilesToExtractAsync();
        installedFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task Broken_WithEmptyOption()
    {
        using var testData = await SetupTestFromDirectoryAsync("Broken-EmptyOption");
        var installedFiles = await testData.GetFilesToExtractAsync();
        installedFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task Broken_WithEmptyStep()
    {
        using var testData = await SetupTestFromDirectoryAsync("Broken-EmptyStep");
        var installedFiles = await testData.GetFilesToExtractAsync();
        installedFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task Broken_WithoutSteps()
    {
        using var testData = await SetupTestFromDirectoryAsync("Broken-NoSteps");
        var installedFiles = await testData.GetFilesToExtractAsync();
        installedFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task Broken_WithoutModuleName()
    {
        using var testData = await SetupTestFromDirectoryAsync("Broken-NoModuleName");
        var installedFiles = await testData.GetFilesToExtractAsync();
        installedFiles.Should().BeEmpty();
    }

    // TODO: Implement Dependencies for FOMODs

    [Fact]
    public async Task Broken_DependencyOnFiles()
    {
        using var testData = await SetupTestFromDirectoryAsync("DependencyOnFiles");
        var installedFiles = await testData.GetFilesToExtractAsync();
        installedFiles.Count().Should().Be(0);
    }
    */
    //#endregion

    /*

    // Note: I'm not mocking here so I can double up the tests as integration tests.
    // it would also be annoying to mock every one given the number of test cases
    // and different configurations with different sets of files we have.
    private async Task<TestState> SetupTestFromDirectoryAsync(string testName)
    {
        var tmpFile = _tmpFileManager.CreateFile(KnownExtensions.Sqlite);

        var installer = new FomodXmlInstaller(_serviceProvider.GetRequiredService<ILogger<FomodXmlInstaller>>(),
            _coreDelegates,
            _serviceProvider.GetRequiredService<IFileSystem>(),
            _serviceProvider.GetRequiredService<TemporaryFileManager>(),
            new GamePath(GameFolderType.Game, ""),
            _serviceProvider
        );

        return new TestState(installer, tmpFile, archive, _store);
    }

    private record TestState(FomodXmlInstaller Installer, TemporaryPath DataStorePath, DownloadId downloadId, IDataStore DataStore) : IDisposable
    {
        public Priority GetPriority() => Installer.GetPriority(new GameInstallation(), AnalysisResults.Contents);
        public async ValueTask<IEnumerable<AModFile>> GetFilesToExtractAsync()
        {
            var mods = (await Installer.GetModsAsync(
                new GameInstallation{ Game = new UnknownGame(GameDomain.From(""), new Version()) },
                ModId.New(),
                AnalysisResults.Hash,
                AnalysisResults.Contents)).ToArray();

            // broken FOMODs return nothing
            return mods.Length == 0
                ? Array.Empty<AModFile>()
                : mods.First().Files.ToArray();
        }

        public void Dispose()
        {
            DataStorePath.Dispose();
        }
    }
    */
}
