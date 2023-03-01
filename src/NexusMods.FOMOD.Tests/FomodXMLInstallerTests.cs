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

        private static string SIMPLE_INSTALLER_XML = """
            <config xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="http://qconsulting.ca/fo3/ModConfig5.0.xsd">
            	<moduleName>Sample Module</moduleName>
            	<installSteps order="Explicit">
            		<installStep name="Step 1">
            			<optionalFileGroups>
            				<group name="Group 1" type="SelectExactlyOne">
            					<plugins order="Explicit">
            						<plugin name="Step 1 Group 1 Plugin 1">
            							<description>First Plugin</description>
            							<files><file source="g1p1f1.esp" destination="g1p1f1.out.esp"/></files>
                                        <typeDescriptor><type name="Optional"/></typeDescriptor>
            						</plugin>
            						<plugin name="Step 1 Group 1 Plugin 2">
            							<description>Second Plugin</description>
            							<files> <file source="g1p2f1.esp" destination="g1p2f1.out.esp"/> </files>
                                        <typeDescriptor><type name="Optional"/></typeDescriptor>
            						</plugin>
            					</plugins>
            				</group>
            			</optionalFileGroups>
            		</installStep>
            		<installStep name="Step 2">
            			<optionalFileGroups>
            				<group name="Group 1" type="SelectExactlyOne">
            					<plugins order="Explicit">
            						<plugin name="Step 2 Group 1 Plugin 1">
            							<description>First Plugin</description>
            							<files> <file source="g2p1f1.esp" destination="g2p1f1.out.esp"/> </files>
                                        <typeDescriptor><type name="Optional"/></typeDescriptor>
            						</plugin>
            						<plugin name="Step 2 Group 1 Plugin 2">
            							<description>Second Plugin</description>
            							<files> <file source="g2p2f1.esp" destination="g2p2f1.out.esp"/> </files>
                                        <typeDescriptor><type name="Optional"/></typeDescriptor>
            						</plugin>
            					</plugins>
            				</group>
            			</optionalFileGroups>
            		</installStep>
            	</installSteps>
            </config>
            """;

        private static string COMPLEX_INSTALLER_XML = """
            <config xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="http://qconsulting.ca/fo3/ModConfig5.0.xsd">
            	<moduleName>Sample Module</moduleName>
            	<installSteps order="Explicit">
            		<installStep name="Step 1">
            			<optionalFileGroups>
            				<group name="Group 1" type="SelectExactlyOne">
            					<plugins order="Explicit">
            						<plugin name="Step 1 Group 1 Plugin 1">
            							<description>First Plugin</description>
            							<files><file source="g1p1f1.esp" destination="g1p1f1.out.esp"/></files>
                                        <typeDescriptor><type name="Optional"/></typeDescriptor>
            						</plugin>
            						<plugin name="Step 1 Group 1 Plugin 2">
            							<description>Second Plugin</description>
            							<files> <file source="g1p2f1.esp" destination="g1p2f1.out.esp"/> </files>
                                        <typeDescriptor><type name="Recommended"/></typeDescriptor>
            						</plugin>
            					</plugins>
            				</group>
            			</optionalFileGroups>
            		</installStep>
            		<installStep name="Step 2">
            			<optionalFileGroups>
            				<group name="Group 1" type="SelectAny">
            					<plugins order="Explicit">
            						<plugin name="Step 2 Group 1 Plugin 1">
            							<description>First Plugin</description>
            							<files> <file source="g2p1f1.esp" destination="g2p1f1.out.esp"/> </files>
                                        <typeDescriptor><type name="Required"/></typeDescriptor>
            						</plugin>
            						<plugin name="Step 2 Group 1 Plugin 2">
            							<description>Second Plugin</description>
            							<files> <file source="g2p2f1.esp" destination="g2p2f1.out.esp"/> </files>
                                        <typeDescriptor><type name="Required"/></typeDescriptor>
            						</plugin>
            					</plugins>
            				</group>
            			</optionalFileGroups>
            		</installStep>
            	</installSteps>
            </config>
            """;

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
            _files = new EntityDictionary<RelativePath, AnalyzedFile>(_store.Object, new List<KeyValuePair<RelativePath, Id>> {
                KeyValuePair.Create(RelativePath.FromParts(new string[] { "fomod", "ModuleConfig.xml" }), IdEmpty.Empty),
                KeyValuePair.Create(RelativePath.FromParts(new string[] { "g1p1f1.esp" }), dummyFile.DataStoreId),
                KeyValuePair.Create(RelativePath.FromParts(new string[] { "g1p2f1.esp" }), dummyFile.DataStoreId),
                KeyValuePair.Create(RelativePath.FromParts(new string[] { "g2p1f1.esp" }), dummyFile.DataStoreId),
                KeyValuePair.Create(RelativePath.FromParts(new string[] { "g2p2f1.esp" }), dummyFile.DataStoreId),
                });

            _store.Setup(_ => _.Get<AnalyzedFile>(It.IsAny<Id64>(), false)).Returns(new AnalyzedArchive
            {
                FileTypes = new FileType[] { },
                SourcePath = (AbsolutePath)"/foo/bar.zip",
                Hash = Hash.Zero,
                Size = Size.Zero,
                Store = _store.Object,
                Contents = _files,
            });
        }

        [Fact()]
        public void WillIgnoreIfMissingScript()
        {
            var files = new EntityDictionary<RelativePath, AnalyzedFile>(_store.Object, new [] {
                KeyValuePair.Create(RelativePath.FromParts(new string[] { "foobar" }), IdEmpty.Empty)
                });

            var prio = _installer.Priority(_gameInstallation, files);

            prio.Should().Be(Priority.None);
        }

        [Fact()]
        public void PriorityHighIfScriptExists()
        {
            var files = new EntityDictionary<RelativePath, AnalyzedFile>(_store.Object, new [] {
                KeyValuePair.Create(RelativePath.FromParts(new string[] { "fomod", "ModuleConfig.xml" }), IdEmpty.Empty)
                });

            var prio = _installer.Priority(_gameInstallation, files);

            prio.Should().Be(Priority.High);
        }

        [Fact()]
        public async void InstallsFilesSimple()
        {
            SetupInstallerScript(SIMPLE_INSTALLER_XML);

            var installedFiles = await _installer.Install(_gameInstallation, Hash.Zero, _files, CancellationToken.None);

            installedFiles.Count().Should().Be(2);
            installedFiles.ElementAt(0).To.FileName.Should().Be((RelativePath)"g1p1f1.out.esp");
            installedFiles.ElementAt(1).To.FileName.Should().Be((RelativePath)"g2p1f1.out.esp");
        }

        [Fact()]
        public async void ObeysTypeDescriptors()
        {
            SetupInstallerScript(COMPLEX_INSTALLER_XML);

            var installedFiles = await _installer.Install(_gameInstallation, Hash.Zero, _files, CancellationToken.None);

            installedFiles.Count().Should().Be(3);
            // in group 1, the second plugin is recommended
            installedFiles.ElementAt(0).To.FileName.Should().Be((RelativePath)"g1p2f1.out.esp");
            // in group 2, both plugins are required
            installedFiles.ElementAt(1).To.FileName.Should().Be((RelativePath)"g2p1f1.out.esp");
            installedFiles.ElementAt(2).To.FileName.Should().Be((RelativePath)"g2p2f1.out.esp");
        }

        private void SetupInstallerScript(string content)
        {
            _fileExtractor.Setup(_ =>
                _.ExtractAllAsync(It.IsAny<AbsolutePath>(), It.IsAny<AbsolutePath>(), It.IsAny<CancellationToken>()))
                .Callback(async (AbsolutePath from, AbsolutePath to, CancellationToken cancel) =>
                {
                    Console.WriteLine($"extracting {from} to {to}");
                    to.Join("fomod").CreateDirectory();
                    await to.Join("fomod", "ModuleConfig.xml").WriteAllTextAsync(content);
                })
                .Returns(Task.FromResult(0));
        }

        private AnalyzedFile MakeFakeAnalyzedFile()
        {
            return new AnalyzedFile {
                Hash = Hash.Zero,
                Size = Size.Zero,
                FileTypes = new FileType[] { },
                Store = _store.Object,
            };
        }
    }
}