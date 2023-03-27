using Xunit;
using NexusMods.FOMOD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Abstractions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Common;
using FluentAssertions;
using NexusMods.FileExtractor.FileSignatures;
using Moq;
using NexusMods.FileExtractor.Extractors;
using Microsoft.Extensions.Logging;
using FomodInstaller.Interface;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.FOMOD.Tests
{
    public class FomodXMLInstallerTests
    {
        private FomodXMLInstaller? _installerImpl;
        private FomodXMLInstaller _installer
        {
            get {
                if (_installerImpl == null)
                {
                    _installerImpl = new FomodXMLInstaller(_loggerInstaller, _store.Object, _tmpFileManager, _fileExtractor.Object, _coreDelegates);
                }
                return _installerImpl;
            }
        }

        private GameInstallation _gameInstallation;
        private Mock<IDataStore> _store;
        private TemporaryFileManager _tmpFileManager;
        private Mock<FileExtractor.FileExtractor> _fileExtractor;
        private ICoreDelegates _coreDelegates;
        private ILogger<FomodXMLInstaller> _loggerInstaller;
        private EntityDictionary<RelativePath, AnalyzedFile> _files;

        private static Task<string> GetTestInstaller(string path)
        {
            return File.ReadAllTextAsync(Path.Join("TextXMLFiles", path + ".xml"), Encoding.UTF8);
        }

        public FomodXMLInstallerTests(ILogger<FileExtractor.FileExtractor> loggerFE,
                                      ILogger<FomodXMLInstaller> loggerInstaller,
                                      TemporaryFileManager tmpFileManager,
                                      ICoreDelegates coreDelegates)
        {
            _store = new Mock<IDataStore>();
            _tmpFileManager = tmpFileManager;
            _coreDelegates = coreDelegates;
            _loggerInstaller = loggerInstaller;
            _fileExtractor = new Mock<FileExtractor.FileExtractor>(loggerFE, new IExtractor[] {
                new MockExtractor()
            });

            _gameInstallation = new GameInstallation();

            var dummyFile = MakeFakeAnalyzedFile();

            var filePairs = new List<KeyValuePair<RelativePath, IId>> {
                KeyValuePair.Create(new RelativePath(Path.Join("fomod", "ModuleConfig.xml" )), IdEmpty.Empty),
                KeyValuePair.Create(new RelativePath(Path.Join("g1p1f1.esp" )), dummyFile.DataStoreId),
                KeyValuePair.Create(new RelativePath(Path.Join("g1p2f1.esp" )), dummyFile.DataStoreId),
                KeyValuePair.Create(new RelativePath(Path.Join("g2p1f1.esp" )), dummyFile.DataStoreId),
                KeyValuePair.Create(new RelativePath(Path.Join("g2p2f1.esp" )), dummyFile.DataStoreId),
                KeyValuePair.Create(new RelativePath(Path.Join("Pass.txt" )), dummyFile.DataStoreId),
                KeyValuePair.Create(new RelativePath(Path.Join("Fail.txt" )), dummyFile.DataStoreId),

            };

            // Create files for the various selection type tests
            var optionStates = new String[]{"NotUsable", "CouldBeUsable", "Optional", "Recommended", "Required"};
            var groupTypes = new String[]{"SelectAny", "SelectAtLeastOne", "SelectAtMostOne", "SelectExactlyOne", "SelectAll"};
            for (var i = 1; i <= 5; i++)
            {
                foreach (var groupType in groupTypes)
                {
                    filePairs.Add(KeyValuePair.Create(new RelativePath(Path.Join("Pass", groupType, $"{optionStates}.txt" )), dummyFile.DataStoreId));
                    filePairs.Add(KeyValuePair.Create(new RelativePath(Path.Join("Pass", groupType, $"{i:00}.txt" )), dummyFile.DataStoreId));
                }
            }

            // Create files for the various plugin (ESP/ESM/etc.) state tests
            var pluginStates = new String[]{"Active", "Inactive", "Missing"};
            foreach (var state in pluginStates)
            {
                foreach (var compareState in pluginStates)
                {
                    filePairs.Add(KeyValuePair.Create(new RelativePath(compareState == state ? Path.Join("Pass", $"{state}.txt" ) : Path.Join("Fail", state, $"{compareState}.txt" )), dummyFile.DataStoreId));
                }
            }

            _files = new EntityDictionary<RelativePath, AnalyzedFile>(_store.Object,  filePairs);

            _store.Setup(_ => _.Get<AnalyzedFile>(It.IsAny<Id64>(), false)).Returns(new AnalyzedArchive
            {
                FileTypes = new FileType[] { },
                SourcePath = AbsolutePath.FromFullPath("/foo/bar.zip"),
                Hash = Hash.Zero,
                Size = Size.Zero,
                Contents = _files,
            });
        }

        [Fact()]
        public void WillIgnoreIfMissingScript()
        {
            var files = new EntityDictionary<RelativePath, AnalyzedFile>(_store.Object, new [] {
                KeyValuePair.Create(new RelativePath("foobar"), IdEmpty.Empty)
            });

            var prio = _installer.Priority(_gameInstallation, files);

            prio.Should().Be(Priority.None);
        }

        [Fact()]
        public void PriorityHighIfScriptExists()
        {
            var files = new EntityDictionary<RelativePath, AnalyzedFile>(_store.Object, new [] {
                KeyValuePair.Create(new RelativePath(Path.Join("fomod", "ModuleConfig.xml")), IdEmpty.Empty)
            });

            var priority = _installer.Priority(_gameInstallation, files);

            priority.Should().Be(Priority.High);
        }

        [Fact()]
        public async void InstallsFiles()
        {
            SetupInstallerScript(await GetTestInstaller("DependencyTypes/Files"));
            var installedFiles = await _installer.Install(_gameInstallation, Hash.Zero, _files, CancellationToken.None);

            installedFiles.Count().Should().Be(2);
            installedFiles.ElementAt(0).To.FileName.Should().Be(new RelativePath("g1p1f1.out.esp"));
            installedFiles.ElementAt(1).To.FileName.Should().Be(new RelativePath("g2p1f1.out.esp"));
        }

        /*
        [Fact()]
        public async void ObeysTypeDescriptors()
        {
            SetupInstallerScript(await GetTestInstaller("DependencyTypes/Files"));

            var installedFiles = await _installer.Install(_gameInstallation, Hash.Zero, _files, CancellationToken.None);

            installedFiles.Count().Should().Be(3);
            // in group 1, the second plugin is recommended
            installedFiles.ElementAt(0).To.FileName.Should().Be(new RelativePath("g1p2f1.out.esp"));
            // in group 2, both plugins are required
            installedFiles.ElementAt(1).To.FileName.Should().Be(new RelativePath("g2p1f1.out.esp"));
            installedFiles.ElementAt(2).To.FileName.Should().Be(new RelativePath("g2p2f1.out.esp"));
        }
        */

        private void SetupInstallerScript(string content)
        {
            _fileExtractor.Setup(_ =>
                _.ExtractAllAsync(It.IsAny<AbsolutePath>(), It.IsAny<AbsolutePath>(), It.IsAny<CancellationToken>()))
                .Callback(async (AbsolutePath from, AbsolutePath to, CancellationToken cancel) =>
                {
                    Console.WriteLine($"extracting {from} to {to}");
                    to.CombineChecked("fomod").CreateDirectory();
                    await to.CombineChecked(Path.Join("fomod", "ModuleConfig.xml")).WriteAllTextAsync(content);
                })
                .Returns(Task.FromResult(0));
        }

        private AnalyzedFile MakeFakeAnalyzedFile()
        {
            return new AnalyzedFile {
                Hash = Hash.Zero,
                Size = Size.Zero,
                FileTypes = new FileType[] { },
                SourcePath = AbsolutePath.FromFullPath("Fake"),
            };
        }
    }
}
