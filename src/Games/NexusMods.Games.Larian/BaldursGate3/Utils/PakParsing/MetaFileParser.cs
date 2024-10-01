using System.Xml;

namespace NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing
{
    public static class MetaLsxParser
    {
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

                    while (reader.Read())
                    {
                        if (reader is { NodeType: XmlNodeType.Element, Name: "attribute" })
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
                                case "Version64":
                                case "Version":
                                    // TODO: Actually parse the version into something we can compare, which will require different handling depending on Version vs Version64
                                    moduleShortDesc.Version = value ?? string.Empty;
                                    break;
                                case "UUID":
                                    moduleShortDesc.Uuid = value ?? string.Empty;
                                    break;
                                case "MD5":
                                    moduleShortDesc.Md5 = value ?? string.Empty;
                                    break;
                            }
                        }
                        else if (reader is { NodeType: XmlNodeType.EndElement, Name: "node" })
                        {
                            break;
                        }
                    }

                    metaFileData.ModuleShortDesc = moduleShortDesc;
                }
                else if (reader is { NodeType: XmlNodeType.Element, Name: "node" } &&
                         reader.GetAttribute("id") == "Dependencies")
                {
                    while (reader.Read())
                    {
                        if (reader is { NodeType: XmlNodeType.Element, Name: "node" } &&
                            reader.GetAttribute("id") == "ModuleShortDesc")
                        {
                            var dependency = new LsxXmlFormat.ModuleShortDesc();

                            while (reader.Read())
                            {
                                if (reader is { NodeType: XmlNodeType.Element, Name: "attribute" })
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
                                        case "Version64":
                                        case "Version":
                                            // TODO: Actually parse the version into something we can compare, which will require different handling depending on Version vs Version64
                                            dependency.Version = value ?? string.Empty;
                                            break;
                                        case "UUID":
                                            dependency.Uuid = value ?? string.Empty;
                                            break;
                                        case "MD5":
                                            dependency.Md5 = value ?? string.Empty;
                                            break;
                                    }
                                }
                                else if (reader is { NodeType: XmlNodeType.EndElement, Name: "node" })
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
