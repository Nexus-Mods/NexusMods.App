using System.Text;
using System.Xml;
using Bannerlord.ModuleManager;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests;

public static class TestHelper
{
    public static Dictionary<RelativePath, byte[]> CreateTestFiles(ModuleInfoExtended moduleInfo)
    {
        var xml = GetXml(moduleInfo);
        var bytes = Encoding.UTF8.GetBytes(xml);

        return new Dictionary<RelativePath, byte[]>
        {
            { "SubModule.xml", bytes }
        };
    }

    public static string GetXml(ModuleInfoExtended moduleInfo)
    {
        var doc = new XmlDocument();

        var xmlDeclaration = doc.CreateXmlDeclaration( "1.0", "UTF-8", null );
        var root = doc.DocumentElement;
        doc.InsertBefore(xmlDeclaration, root);

        var module = doc.CreateElement(string.Empty, "Module", string.Empty);
        doc.AppendChild(module);

        var id = doc.CreateElement(string.Empty, "Id", string.Empty);
        id.SetAttribute("value", moduleInfo.Id);
        module.AppendChild(id);

        var name = doc.CreateElement(string.Empty, "Name", string.Empty);
        name.SetAttribute("value", moduleInfo.Name);
        module.AppendChild(name);

        var version = doc.CreateElement(string.Empty, "Version", string.Empty);
        version.SetAttribute("value", moduleInfo.Version.ToString());
        module.AppendChild(version);

        var defaultModule = doc.CreateElement(string.Empty, "DefaultModule", string.Empty);
        defaultModule.SetAttribute("value", moduleInfo.IsOfficial ? "false" : "true");
        module.AppendChild(defaultModule);

        var moduleCategory = doc.CreateElement(string.Empty, "ModuleCategory", string.Empty);
        moduleCategory.SetAttribute("value", moduleInfo.IsSingleplayerModule ? "Singleplayer" : moduleInfo.IsMultiplayerModule ? "Multiplayer" : moduleInfo.IsServerModule ? "Server" : "None");
        module.AppendChild(moduleCategory);

        var moduleType = doc.CreateElement(string.Empty, "ModuleType", string.Empty);
        moduleType.SetAttribute("value", moduleInfo.IsOfficial ? "Official" : "Community");
        module.AppendChild(moduleType);

        var url = doc.CreateElement(string.Empty, "Url", string.Empty);
        url.SetAttribute("value", moduleInfo.Url);
        module.AppendChild(url);

        var dependedModuleMetadatas = doc.CreateElement(string.Empty, "DependedModuleMetadatas", string.Empty);
        module.AppendChild(dependedModuleMetadatas);
        foreach (var dependency in moduleInfo.DependenciesToLoadDistinct())
        {
            var dep = doc.CreateElement(string.Empty, "DependedModuleMetadata", string.Empty);
            dep.SetAttribute("id", dependency.Id);
            dep.SetAttribute("order", dependency.LoadType.ToString());
            dep.SetAttribute("optional", dependency.IsOptional ? "true" : "false");
            dependedModuleMetadatas.AppendChild(dep);
        }

        var subModules = doc.CreateElement(string.Empty, "SubModules", string.Empty);
        module.AppendChild(subModules);
        foreach (var subModule in moduleInfo.SubModules)
        {
            var sub = doc.CreateElement(string.Empty, "SubModule", string.Empty);

            var subName = doc.CreateElement(string.Empty, "Name", string.Empty);
            subName.SetAttribute("value", subModule.Name);
            sub.AppendChild(subName);

            var dllName = doc.CreateElement(string.Empty, "DLLName", string.Empty);
            dllName.SetAttribute("value", subModule.DLLName);
            sub.AppendChild(dllName);

            var subModuleClassType = doc.CreateElement(string.Empty, "SubModuleClassType", string.Empty);
            dllName.SetAttribute("value", subModule.SubModuleClassType);
            sub.AppendChild(subModuleClassType);

            var assemblies = doc.CreateElement(string.Empty, "Assemblies", string.Empty);
            foreach (var assembly in subModule.Assemblies)
            {
                var ass = doc.CreateElement(string.Empty, "Assembly", string.Empty);
                ass.SetAttribute("value", assembly);
                assemblies.AppendChild(ass);
            }
        }

        return doc.OuterXml;
    }
}
