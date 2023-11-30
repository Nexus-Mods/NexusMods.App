namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Shared;

public static class Data
{
        public static readonly string HarmonySubModuleXml = """
<?xml version="1.0" encoding="UTF-8"?>
<Module xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
        xsi:noNamespaceSchemaLocation="https://raw.githubusercontent.com/BUTR/Bannerlord.XmlSchemas/master/SubModule.xsd" >
  <Id value="Bannerlord.Harmony" />
  <Name value="Harmony" />
  <Version value="v2.2.0.0" />
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

        public static readonly string ButterLibSubModuleXml = """
<?xml version="1.0" encoding="UTF-8"?>
<Module xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
        xsi:noNamespaceSchemaLocation="https://raw.githubusercontent.com/BUTR/Bannerlord.XmlSchemas/master/SubModule.xsd" >
  <Id value="Bannerlord.ButterLib" />
  <Name value="ButterLib" />
  <Version value="v2.8.15" />
  <DefaultModule value="false" />
  <ModuleCategory value="Singleplayer" />
  <ModuleType value="Community"/>
  <Url value="https://www.nexusmods.com/mountandblade2bannerlord/mods/2018" />
  <UpdateInfo value="NexusMods:2018" />
  <DependedModules>
    <!-- Uncomment when doing ExtensionVersion > 2.0.0
    <DependedModule Id="BLSE.AssemblyResolver" />
    -->
    <DependedModule Id="Bannerlord.Harmony" DependentVersion="v2.2.2" />
  </DependedModules>
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
    <DependedModuleMetadata id="Bannerlord.Harmony" order="LoadBeforeThis" version="v2.2.2" />
    <DependedModuleMetadata id="BetterExceptionWindow" order="LoadBeforeThis" optional="true" />

    <DependedModuleMetadata id="Native" order="LoadAfterThis" version="v1.0.0.*" />
    <DependedModuleMetadata id="SandBoxCore" order="LoadAfterThis" version="v1.0.0.*" optional="true" />
    <DependedModuleMetadata id="Sandbox" order="LoadAfterThis" version="v1.0.0.*" optional="true" />
    <DependedModuleMetadata id="StoryMode" order="LoadAfterThis" version="v1.0.0.*" optional="true" />
    <DependedModuleMetadata id="CustomBattle" order="LoadAfterThis" version="v1.0.0.*" optional="true" />
  </DependedModuleMetadatas>
  <!-- Community Dependency Metadata -->
  <SubModules>
    <SubModule>
      <Name value="ButterLib" />
      <DLLName value="Bannerlord.ButterLib.dll" />
      <SubModuleClassType value="Bannerlord.ButterLib.ButterLibSubModule" />
      <Assemblies>
        <!--Helper libraries-->
        <Assembly value="Microsoft.Bcl.HashCode.dll" />
        <!--ILogger Implementation Serilog-->
        <Assembly value="Serilog.dll" />
        <Assembly value="Serilog.Extensions.Logging.dll" />
        <Assembly value="Serilog.Sinks.File.dll" />
      </Assemblies>
      <Tags />
    </SubModule>
    <SubModule>
      <Name value="ButterLib Implementation Loader" />
      <DLLName value="Bannerlord.ButterLib.dll" />
      <SubModuleClassType value="Bannerlord.ButterLib.ImplementationLoaderSubModule" />
      <Assemblies/>
      <Tags />
    </SubModule>
  </SubModules>
</Module>
""";
}
