using System.Text;
using FluentAssertions;
using NexusMods.Games.MountAndBlade2Bannerlord.Installers;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests;

public class MountAndBlade2BannerlordModInstallerTests : AModInstallerTest<MountAndBlade2Bannerlord, MountAndBlade2BannerlordModInstaller>
{
    private static string SubModuleXml = """
<?xml version="1.0" encoding="UTF-8"?>
<Module xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
        xsi:noNamespaceSchemaLocation="https://raw.githubusercontent.com/BUTR/Bannerlord.XmlSchemas/master/SubModule.xsd" >
  <Id value="Bannerlord.Harmony" />
  <Name value="Harmony" />
  <Version value="v2.3.0.0" />
  <DefaultModule value="false" />
  <ModuleCategory value="Singleplayer" />
  <ModuleType value ="Community" />
  <Url value="https://www.nexusmods.com/mountandblade2bannerlord/mods/2006" />
  <DependedModules />
  <ModulesToLoadAfterThis>
    <Module Id="Native" />
    <Module Id="SandBoxCore" />
    <Module Id="Sandbox" />
    <Module Id="StoryMode" />
    <Module Id="CustomBattle" />
  </ModulesToLoadAfterThis>
  <!-- Community Dependency Metadata -->
  <!-- https://github.com/BUTR/Bannerlord.BUTRLoader#for-modders -->
  <DependedModuleMetadatas>
    <DependedModuleMetadata id="Native" order="LoadAfterThis" optional="true" />
    <DependedModuleMetadata id="SandBoxCore" order="LoadAfterThis" optional="true" />
    <DependedModuleMetadata id="Sandbox" order="LoadAfterThis" optional="true" />
    <DependedModuleMetadata id="StoryMode" order="LoadAfterThis" optional="true" />
    <DependedModuleMetadata id="CustomBattle" order="LoadAfterThis" optional="true" />
  </DependedModuleMetadatas>
  <!-- Community Dependency Metadata -->
  <SubModules>
    <SubModule>
      <Name value="Harmony" />
      <DLLName value="Bannerlord.Harmony.dll" />
      <SubModuleClassType value="Bannerlord.Harmony.SubModule" />
      <Tags/>
    </SubModule>
  </SubModules>
</Module>
""";
    
    public MountAndBlade2BannerlordModInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_WithRealMod()
    {
        // Harmony: Harmony Main File (Version 2.3.0)
        var (file, hash) = await DownloadMod(Game.Domain, ModId.From(2006), FileId.From(34666));
        hash.Should().Be(Hash.From(0x3FD3503D19DAE052));

        await using (file)
        {
            var filesToExtract = await GetFilesToExtractFromInstaller(file.Path);
            filesToExtract.Should().HaveCount(49);
            filesToExtract.Should().AllSatisfy(x => x.To.Path.StartsWith("Modules/Test"));
            filesToExtract.Should().Contain(x => x.To.FileName == "Bannerlord.Harmony.dll");
            filesToExtract.Should().Contain(x => x.To.FileName == "SubModule.xml");
        }
    }
    
    [Fact]
    public async Task Test_WithFakeMod()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>();
        testFiles["Test/SubModule.xml"] = Encoding.UTF8.GetBytes(SubModuleXml);
        testFiles["Test/bin/Win64_Shipping_Client/Bannerlord.Harmony.dll"] = Array.Empty<byte>();
        testFiles["Test/bin/Gaming.Desktop.x64_Shipping_Client/Bannerlord.Harmony.dll"] = Array.Empty<byte>();

        var file = await CreateTestArchive(testFiles);
        await using (file)
        {
            var filesToExtract = await GetFilesToExtractFromInstaller(file.Path);
            filesToExtract.Should().HaveCount(3);
            filesToExtract.Should().AllSatisfy(x => x.To.Path.StartsWith("Modules/Test"));
            filesToExtract.Should().Contain(x => x.To.FileName == "Bannerlord.Harmony.dll");
            filesToExtract.Should().Contain(x => x.To.FileName == "SubModule.xml");
        }
    }    
    [Fact]
    public async Task Test_WithFakeMod_WithModulesRoot()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>();
        testFiles["Modules/Test/SubModule.xml"] = Encoding.UTF8.GetBytes(SubModuleXml);
        testFiles["Modules/Test/bin/Win64_Shipping_Client/Bannerlord.Harmony.dll"] = Array.Empty<byte>();
        testFiles["Modules/Test/bin/Gaming.Desktop.x64_Shipping_Client/Bannerlord.Harmony.dll"] = Array.Empty<byte>();

        var file = await CreateTestArchive(testFiles);
        await using (file)
        {
            var filesToExtract = await GetFilesToExtractFromInstaller(file.Path);
            filesToExtract.Should().HaveCount(3);
            filesToExtract.Should().AllSatisfy(x => x.To.Path.StartsWith("Modules/Test"));
            filesToExtract.Should().Contain(x => x.To.FileName == "Bannerlord.Harmony.dll");
            filesToExtract.Should().Contain(x => x.To.FileName == "SubModule.xml");
        }
    }
}
