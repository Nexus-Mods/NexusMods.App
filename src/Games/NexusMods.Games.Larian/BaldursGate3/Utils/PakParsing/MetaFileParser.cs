using System;
using System.IO;
using System.Xml;

namespace NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;



public class XmlExtractor
{
    public static void ExtractAttributes(Stream xmlStream)
    {
        using (var reader = XmlReader.Create(xmlStream))
        {
            string? folder = null;
            string? name = null;
            string? publishHandle = null;
            string? version64 = null;
            string? uuid = null;
            string? md5 = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "node" && reader.GetAttribute("id") == "ModuleInfo")
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "attribute")
                        {
                            var id = reader.GetAttribute("id");
                            var value = reader.GetAttribute("value");

                            switch (id)
                            {
                                case "Folder":
                                    folder = value;
                                    break;
                                case "Name":
                                    name = value;
                                    break;
                                case "PublishHandle":
                                    publishHandle = value;
                                    break;
                                case "Version64":
                                    version64 = value;
                                    break;
                                case "UUID":
                                    uuid = value;
                                    break;
                                case "MD5":
                                    md5 = value;
                                    break;
                            }
                        }
                        else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "node")
                        {
                            break;
                        }
                    }
                }
            }

            Console.WriteLine($"Folder: {folder}");
            Console.WriteLine($"Name: {name}");
            Console.WriteLine($"PublishHandle: {publishHandle}");
            Console.WriteLine($"Version64: {version64}");
            Console.WriteLine($"UUID: {uuid}");
            Console.WriteLine($"MD5: {md5}");
        }
    }
}
