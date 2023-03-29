using FluentAssertions;
using FomodInstaller.Interface;
using Microsoft.Extensions.Logging;
using Moq;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.FileExtractor.Extractors;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Games.FOMOD.Tests.Mocks;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.TestingHelpers;
using Xunit;

namespace NexusMods.Games.FOMOD.Tests;

public class FomodXmlInstallerTests
{
    private FomodXmlInstaller? _installerImpl;
    private FomodXmlInstaller _installer =>
        _installerImpl ??= new FomodXmlInstaller(_loggerInstaller, _store.Object,
            _tmpFileManager, _fileExtractor.Object, _coreDelegates);

    private GameInstallation _gameInstallation;
    private Mock<IDataStore> _store;
    private TemporaryFileManager _tmpFileManager;
    private Mock<FileExtractor.FileExtractor> _fileExtractor;
    private ICoreDelegates _coreDelegates;
    private ILogger<FomodXmlInstaller> _loggerInstaller;
    private EntityDictionary<RelativePath, AnalyzedFile> _files;

    // ReSharper disable once ContextualLoggerProblem
    public FomodXmlInstallerTests(ILogger<FileExtractor.FileExtractor> loggerFe,
        // ReSharper disable once ContextualLoggerProblem
        ILogger<FomodXmlInstaller> loggerInstaller,
        TemporaryFileManager tmpFileManager,
        ICoreDelegates coreDelegates)
    {
        _store = new Mock<IDataStore>();
        _tmpFileManager = tmpFileManager;
        _coreDelegates = coreDelegates;
        _loggerInstaller = loggerInstaller;
        _fileExtractor = new Mock<FileExtractor.FileExtractor>(loggerFe, new IExtractor[] {
            new MockExtractor()
        });

        _gameInstallation = new GameInstallation();

        var dummyFile = MakeFakeAnalyzedFile();
        _files = new EntityDictionary<RelativePath, AnalyzedFile>(_store.Object, new List<KeyValuePair<RelativePath, IId>> {
            KeyValuePair.Create("fomod/ModuleConfig.xml".ToRelativePath(), IdEmpty.Empty),
            KeyValuePair.Create("g1p1f1.esp".ToRelativePath(), dummyFile.DataStoreId),
            KeyValuePair.Create("g1p2f1.esp".ToRelativePath(), dummyFile.DataStoreId),
            KeyValuePair.Create("g2p1f1.esp".ToRelativePath(), dummyFile.DataStoreId),
            KeyValuePair.Create("g2p2f1.esp".ToRelativePath(), dummyFile.DataStoreId),
        });

        _store.Setup(_ => _.Get<AnalyzedFile>(It.IsAny<Id64>(), false)).Returns(new AnalyzedArchive
        {
            FileTypes = new FileType[] { },
            Hash = Hash.Zero,
            Size = Size.Zero,
            Contents = _files,
        });
    }

    [Fact]
    public void WillIgnoreIfMissingScript()
    {
        var files = new EntityDictionary<RelativePath, AnalyzedFile>(_store.Object, new [] {
            KeyValuePair.Create("foobar".ToRelativePath(), IdEmpty.Empty)
        });

        var prio = _installer.Priority(_gameInstallation, files);

        prio.Should().Be(Priority.None);
    }

    [Fact]
    public void PriorityHighIfScriptExists()
    {
        var files = new EntityDictionary<RelativePath, AnalyzedFile>(_store.Object, new [] {
            KeyValuePair.Create("fomod/ModuleConfig.xml".ToRelativePath(), IdEmpty.Empty)
        });

        var prio = _installer.Priority(_gameInstallation, files);

        prio.Should().Be(Priority.High);
    }

    [Theory, AutoFileSystem]
    public async void InstallsFilesSimple(InMemoryFileSystem fileSystem)
    {
        SetupInstallerScriptFromFile("SimpleInstaller.xml", fileSystem);

        var installedFiles = await _installer.InstallAsync(_gameInstallation, Hash.Zero, _files, CancellationToken.None);

        installedFiles.Count().Should().Be(2);
        installedFiles.ElementAt(0).To.FileName.Should().Be((RelativePath)"g1p1f1.out.esp");
        installedFiles.ElementAt(1).To.FileName.Should().Be((RelativePath)"g2p1f1.out.esp");
    }

    [Theory, AutoFileSystem]
    public async void ObeysTypeDescriptors(InMemoryFileSystem fileSystem)
    {
        SetupInstallerScriptFromFile("SimpleInstaller.xml", fileSystem);

        var installedFiles = await _installer.InstallAsync(_gameInstallation, Hash.Zero, _files, CancellationToken.None);

        installedFiles.Count().Should().Be(3);

        // In group 1, the second plugin is recommended
        installedFiles.ElementAt(0).To.FileName.Should().Be("g1p2f1.out.esp".ToRelativePath());

        // In group 2, both plugins are required
        installedFiles.ElementAt(1).To.FileName.Should().Be("g2p1f1.out.esp".ToRelativePath());
        installedFiles.ElementAt(2).To.FileName.Should().Be("g2p2f1.out.esp".ToRelativePath());
    }

    private void SetupInstallerScriptFromFile(string fileName, IFileSystem fileSystem)
    {
        var path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).CombineUnchecked($"Assets/{fileName}").GetFullPath();
        var text = File.ReadAllText(path);
        _fileExtractor.Setup(_ =>
                _.ExtractAllAsync(It.IsAny<AbsolutePath>(), It.IsAny<AbsolutePath>(), It.IsAny<CancellationToken>()))
            .Callback(async (AbsolutePath from, AbsolutePath to, CancellationToken cancel) =>
            {
                Console.WriteLine($"extracting {from} to {to}");
                to.CombineUnchecked("fomod").CreateDirectory();
                await to.CombineUnchecked("fomod/ModuleConfig.xml").WriteAllTextAsync(fileName);
            })
            .Returns(Task.FromResult(0));
    }

    private AnalyzedFile MakeFakeAnalyzedFile()
    {
        return new AnalyzedFile {
            Hash = Hash.Zero,
            Size = Size.Zero,
            FileTypes = new FileType[] { },
        };
    }
}
