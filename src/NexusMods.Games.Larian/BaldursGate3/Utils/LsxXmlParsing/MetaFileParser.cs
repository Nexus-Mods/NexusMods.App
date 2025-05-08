using System.Xml;

namespace NexusMods.Games.Larian.BaldursGate3.Utils.LsxXmlParsing
{
    /// <summary>
    /// Class to parse and extract dependencies and metadata from a bg3 `meta.lsx` file.
    /// </summary>
    public static class MetaLsxParser
    {
        
        /// <summary>
        /// Parses dependencies and metadata from a stream of a bg3 `meta.lsx` file.
        /// </summary>
        /// <param name="xmlStream"></param>
        /// <returns></returns>
        public static LsxXmlFormat.MetaFileData ParseMetaFile(Stream xmlStream)
        {
            using var reader = XmlReader.Create(xmlStream);
            var metaFileData = new LsxXmlFormat.MetaFileData();
            var dependencies = new List<LsxXmlFormat.ModuleShortDesc>();

            while (reader.Read())
            {
                if (reader is { NodeType: XmlNodeType.Element, Name: "node" } &&
                    reader.GetAttribute("id") == "ModuleInfo")
                {
                    var moduleShortDesc = new LsxXmlFormat.ModuleShortDesc();
                    var moduleInfoDepth = reader.Depth;

                    while (reader.Read())
                    {
                        if (reader is { NodeType: XmlNodeType.Element, Name: "attribute" } && 
                            reader.Depth == moduleInfoDepth + 1)
                        {
                            var id = reader.GetAttribute("id");
                            var value = reader.GetAttribute("value");

                            switch (id)
                            {
                                case "Folder":
                                    moduleShortDesc.Folder = value ?? string.Empty;
                                    break;
                                case "Name":
                                    moduleShortDesc.Name = value ?? string.Empty;
                                    break;
                                case "PublishHandle":
                                    moduleShortDesc.PublishHandle = value ?? string.Empty;
                                    break;
                                case "Version":
                                    // From older meta files, it seems that it could contain either 32-bit or 64-bit values even though type is marked as Int32
                                    // They later changed it to Version64, to clarify the type, but older mods can still have this
                                    moduleShortDesc.Version = value ?? string.Empty;
                                    moduleShortDesc.SemanticVersion = LsxXmlFormat.ModuleVersion.FromInt32String(moduleShortDesc.Version);
                                    break;
                                case "Version64":
                                    moduleShortDesc.Version = value ?? string.Empty;
                                    moduleShortDesc.SemanticVersion = LsxXmlFormat.ModuleVersion.FromInt64String(moduleShortDesc.Version);
                                    break;
                                case "UUID":
                                    moduleShortDesc.Uuid = value ?? string.Empty;
                                    break;
                                case "MD5":
                                    moduleShortDesc.Md5 = value ?? string.Empty;
                                    break;
                            }
                        }
                        else if (reader is { NodeType: XmlNodeType.EndElement, Name: "node" } && reader.Depth == moduleInfoDepth)
                        {
                            break;
                        }
                    }

                    metaFileData.ModuleShortDesc = moduleShortDesc;
                }
                else if (reader is { NodeType: XmlNodeType.Element, Name: "node" } &&
                         reader.GetAttribute("id") == "Dependencies")
                {
                    if (reader.IsEmptyElement)
                    {
                        continue;
                    }
                    
                    while (reader.Read())
                    {
                        if (reader is { NodeType: XmlNodeType.Element, Name: "node" } &&
                            reader.GetAttribute("id") == "ModuleShortDesc")
                        {
                            var dependency = new LsxXmlFormat.ModuleShortDesc();
                            var dependencyDepth = reader.Depth;

                            while (reader.Read())
                            {
                                if (reader is { NodeType: XmlNodeType.Element, Name: "attribute" } &&
                                    reader.Depth == dependencyDepth + 1)
                                {
                                    var id = reader.GetAttribute("id");
                                    var value = reader.GetAttribute("value");

                                    switch (id)
                                    {
                                        case "Folder":
                                            dependency.Folder = value ?? string.Empty;
                                            break;
                                        case "Name":
                                            dependency.Name = value ?? string.Empty;
                                            break;
                                        case "PublishHandle":
                                            dependency.PublishHandle = value ?? string.Empty;
                                            break;
                                        case "Version":
                                            // From older meta files, it seems that it could contain either 32-bit or 64-bit values even though type is marked as Int32
                                            // They later changed it to Version64, to clarify the type, but older mods can still have this
                                            dependency.Version = value ?? string.Empty;
                                            dependency.SemanticVersion = LsxXmlFormat.ModuleVersion.FromInt32String(dependency.Version);
                                            break;
                                        case "Version64":
                                            dependency.Version = value ?? string.Empty;
                                            dependency.SemanticVersion = LsxXmlFormat.ModuleVersion.FromInt64String(dependency.Version);
                                            break;
                                        case "UUID":
                                            dependency.Uuid = value ?? string.Empty;
                                            break;
                                        case "MD5":
                                            dependency.Md5 = value ?? string.Empty;
                                            break;
                                    }
                                }
                                else if (reader is { NodeType: XmlNodeType.EndElement, Name: "node" } && reader.Depth == dependencyDepth)
                                {
                                    break;
                                }
                            }

                            if (LsxXmlFormat.DependencyUuidsToIgnore.Contains(dependency.Uuid))
                                continue;
                            dependencies.Add(dependency);
                        }
                        else if (reader is { NodeType: XmlNodeType.EndElement, Name: "node" })
                        {
                            break;
                        }
                    }
                }
            }

            metaFileData.Dependencies = dependencies.ToArray();
            return metaFileData;
        }
    }
}
