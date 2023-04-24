using FluentAssertions;
using FomodInstaller.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Interprocess;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;
using Xunit;
using Mod = NexusMods.DataModel.Loadouts.Mod;

namespace NexusMods.Games.FOMOD.Tests;

public class FomodXmlInstallerTests
{
    private TemporaryFileManager _tmpFileManager;
    private ICoreDelegates _coreDelegates;

    private readonly IServiceProvider _serviceProvider;

    public FomodXmlInstallerTests(IServiceProvider serviceProvider,
        TemporaryFileManager tmpFileManager,
        ICoreDelegates coreDelegates)
    {
        _serviceProvider = serviceProvider;
        _tmpFileManager = tmpFileManager;
        _coreDelegates = coreDelegates;
    }

    [Fact]
    public async Task WillIgnoreIfMissingScript()
    {
        using var testCase = await SetupTestFromDirectoryAsync("NotAFomod");
        var prio = testCase.GetPriority();
        prio.Should().Be(Priority.None);
    }

    [Fact]
    public async Task PriorityHighIfScriptExists()
    {
        using var testCase = await SetupTestFromDirectoryAsync("SimpleInstaller");
        var prio = testCase.GetPriority();
        prio.Should().Be(Priority.High);
    }

    [Fact]
    public async Task InstallsFilesSimple()
    {
        using var testData = await SetupTestFromDirectoryAsync("SimpleInstaller");
        var installedFiles = (await testData.GetFilesToExtractAsync()).ToArray();

        installedFiles
            .Should().HaveCount(2)
            .And.Satisfy(
                x => x.To.FileName == "g1p1f1.out.esp",
                x => x.To.FileName == "g2p1f1.out.esp"
            );
    }

    [Fact]
    public async Task InstallsFilesComplex_WithImages()
    {
        using var testData = await SetupTestFromDirectoryAsync("WithImages");
        var installedFiles = (await testData.GetFilesToExtractAsync()).ToArray();

        installedFiles
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
    public async Task InstallsFilesComplex_WithMissingImage()
    {
        using var testData = await SetupTestFromDirectoryAsync("WithMissingImage");
        var installedFiles = (await testData.GetFilesToExtractAsync()).ToArray();

        installedFiles
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
            .Should().HaveCount(2)
            .And.Satisfy(
                x => x.To.FileName == "g1p1f1.out.esp",
                x => x.To.FileName == "g2p1f1.out.esp"
            );
    }

    [Fact]
    public async Task ObeysTypeDescriptors()
    {
        using var testData = await SetupTestFromDirectoryAsync("ComplexInstaller");
        var installedFiles = (await testData.GetFilesToExtractAsync()).ToArray();

        installedFiles
            .Should().HaveCount(3)
            .And.Satisfy(
                // In group 1, the second plugin is recommended
                x => x.To.FileName == "g1p2f1.out.esp",
                // In group 2, both plugins are required
                x => x.To.FileName == "g2p1f1.out.esp",
                x => x.To.FileName == "g2p2f1.out.esp"
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
    /*
    [Fact]
    public async Task Broken_DependencyOnFiles()
    {
        using var testData = await SetupTestFromDirectoryAsync("DependencyOnFiles");
        var installedFiles = await testData.GetFilesToExtractAsync();
        installedFiles.Count().Should().Be(0);
    }
    */
    #endregion

    // Note: I'm not mocking here so I can double up the tests as integration tests.
    // it would also be annoying to mock every one given the number of test cases
    // and different configurations with different sets of files we have.
    private async Task<TestState> SetupTestFromDirectoryAsync(string testName)
    {
        var tmpFile = _tmpFileManager.CreateFile(KnownExtensions.Sqlite);

        var dataStore = new SqliteDataStore(
            _serviceProvider.GetRequiredService<ILogger<SqliteDataStore>>(),
            new DataModelSettings(FileSystem.Shared), 
            _serviceProvider,
            _serviceProvider.GetRequiredService<IMessageProducer<IdUpdated>>(),
            _serviceProvider.GetRequiredService<IMessageConsumer<IdUpdated>>());

        var installer = new FomodXmlInstaller(
            _serviceProvider.GetRequiredService<IDataStore>(),
            _serviceProvider.GetRequiredService<ILogger<FomodXmlInstaller>>(),
            _coreDelegates
        );

        var analyzer = new FomodAnalyzer(
            _serviceProvider.GetRequiredService<ILogger<FomodAnalyzer>>(),
            FileSystem.Shared);

        var contentsCache = new FileContentsCache(
            _serviceProvider.GetRequiredService<ILogger<FileContentsCache>>(),
            _serviceProvider.GetRequiredService<IResource<FileContentsCache, Size>>(),
            _serviceProvider.GetRequiredService<FileExtractor.FileExtractor>(),
            _serviceProvider.GetRequiredService<TemporaryFileManager>(),
            new FileHashCache(_serviceProvider.GetRequiredService<IResource<FileHashCache, Size>>(), dataStore),
            new IFileAnalyzer[] { analyzer },
            dataStore
        );

        var analyzed = await contentsCache.AnalyzeFileAsync(FomodTestHelpers.GetFomodPath(testName));
        if (analyzed is not AnalyzedArchive archive)
            throw new Exception("FOMOD was not registered as archive.");

        return new TestState(installer, tmpFile, archive, dataStore);
    }

    private record TestState(FomodXmlInstaller Installer, TemporaryPath DataStorePath, AnalyzedArchive AnalysisResults, SqliteDataStore DataStore) : IDisposable
    {
        public Priority GetPriority() => Installer.GetPriority(new GameInstallation(), AnalysisResults.Contents);
        public async ValueTask<IEnumerable<AModFile>> GetFilesToExtractAsync()
        {
            var mods = (await Installer.GetModsAsync(
                new GameInstallation(),
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
            DataStore.Dispose();
            DataStorePath.Dispose();
        }
    }
}
