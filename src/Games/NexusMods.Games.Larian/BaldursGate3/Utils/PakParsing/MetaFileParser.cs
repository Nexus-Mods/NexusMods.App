using System.Xml;

namespace NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing
{
    public class MetaLsxParser
    {
        public static LsxXmlFormat.ModuleShortDesc ParseMetaFile(Stream xmlStream)
        {
            using var reader = XmlReader.Create(xmlStream);
            while (reader.Read())
            {
                if (reader is not { NodeType: XmlNodeType.Element, Name: "node" } || 
                    reader.GetAttribute("id") != "ModuleInfo")
                {
                    continue;
                }
                
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

                Console.WriteLine($"Folder: {moduleShortDesc.Folder}");
                Console.WriteLine($"Name: {moduleShortDesc.Name}");
                Console.WriteLine($"PublishHandle: {moduleShortDesc.PublishHandle}");
                Console.WriteLine($"Version: {moduleShortDesc.Version}");
                Console.WriteLine($"UUID: {moduleShortDesc.Uuid}");
                Console.WriteLine($"MD5: {moduleShortDesc.Md5}");
                    
                return moduleShortDesc;
            }

            // If we reach here, we didn't find the ModuleInfo node
            throw new InvalidDataException("Could not find the ModuleInfo node in the LSX file.");
        }
    }
}
