using System.Xml;

namespace NexusMods.Games.Larian.BaldursGate3.Utils.LsxXmlParsing;

/// <summary>
/// Class to parse and write the `modsettings.lsx` xml file format used for the BG3 Load Order.
/// </summary>
public static class ModsettingsFileFormat
{
    public static string SerializeModsettingsLoadOrder(LsxXmlFormat.ModuleShortDesc[] moduleShortDescs)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false,
        };

        using var stringWriter = new StringWriter();
        using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
        {
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("save");

            WriteVersion(xmlWriter);
            WriteRegion(xmlWriter, moduleShortDescs);

            xmlWriter.WriteEndElement(); // save
            xmlWriter.WriteEndDocument();
        }

        return stringWriter.ToString();
    }

    public static LsxXmlFormat.ModuleShortDesc[] DeserializeModsettingsLoadOrder(string modsettingsXml)
    {
        var modulesLoadOrder = new List<LsxXmlFormat.ModuleShortDesc>();
        var skipFirstGustavDev = true;

        using (var stringReader = new StringReader(modsettingsXml))
        using (var xmlReader = XmlReader.Create(stringReader))
        {
            while (xmlReader.Read())
            {
                if (xmlReader is not { NodeType: XmlNodeType.Element, Name: "node" } ||
                    xmlReader.GetAttribute("id") != "ModuleShortDesc")
                {
                    continue;
                }

                var moduleShortDesc = new LsxXmlFormat.ModuleShortDesc();

                while (xmlReader.Read() && xmlReader is not { NodeType: XmlNodeType.EndElement, Name: "node" })
                {
                    if (xmlReader is { NodeType: XmlNodeType.Element, Name: "attribute" })
                    {
                        var id = xmlReader.GetAttribute("id");
                        var value = xmlReader.GetAttribute("value");

                        switch (id)
                        {
                            case "Folder":
                                moduleShortDesc.Folder = value ?? string.Empty;
                                break;
                            case "MD5":
                                moduleShortDesc.Md5 = value ?? string.Empty;
                                break;
                            case "Name":
                                moduleShortDesc.Name = value ?? string.Empty;
                                break;
                            case "PublishHandle":
                                moduleShortDesc.PublishHandle = value ?? string.Empty;
                                break;
                            case "UUID":
                                moduleShortDesc.Uuid = value ?? string.Empty;
                                break;
                            case "Version64":
                                moduleShortDesc.Version = value ?? string.Empty;
                                break;
                        }
                    }
                }

                // Skip the GustavDev entry
                if (skipFirstGustavDev && moduleShortDesc is { Name: "GustavDev" })
                {
                    skipFirstGustavDev = false;
                    continue;
                }

                modulesLoadOrder.Add(moduleShortDesc);
            }
        }

        return modulesLoadOrder.ToArray();
    }

#region private methods

    private static void WriteVersion(XmlWriter xmlWriter)
    {
        xmlWriter.WriteStartElement("version");
        xmlWriter.WriteAttributeString("major", "4");
        xmlWriter.WriteAttributeString("minor", "7");
        xmlWriter.WriteAttributeString("revision", "1");
        xmlWriter.WriteAttributeString("build", "200");
        xmlWriter.WriteEndElement();
    }

    private static void WriteRegion(XmlWriter xmlWriter, LsxXmlFormat.ModuleShortDesc[] moduleShortDescs)
    {
        xmlWriter.WriteStartElement("region");
        xmlWriter.WriteAttributeString("id", "ModuleSettings");

        xmlWriter.WriteStartElement("node");
        xmlWriter.WriteAttributeString("id", "root");

        xmlWriter.WriteStartElement("children");
        xmlWriter.WriteStartElement("node");
        xmlWriter.WriteAttributeString("id", "Mods");

        xmlWriter.WriteStartElement("children");

        // Add default GustavDev entry
        WriteModuleShortDesc(xmlWriter,
            new LsxXmlFormat.ModuleShortDesc
            {
                Folder = "GustavDev",
                Name = "GustavDev",
                PublishHandle = "0",
                Version = "36028797018963968",
                Uuid = "28ac9ce2-2aba-8cda-b3b5-6e922f71b6b8",
                Md5 = ""
            }
        );

        // Add pak mod entries
        foreach (var moduleShortDesc in moduleShortDescs)
        {
            WriteModuleShortDesc(xmlWriter, moduleShortDesc);
        }

        xmlWriter.WriteEndElement(); // children
        xmlWriter.WriteEndElement(); // node Mods
        xmlWriter.WriteEndElement(); // children
        xmlWriter.WriteEndElement(); // node root
        xmlWriter.WriteEndElement(); // region
    }

    internal static void WriteModuleShortDesc(XmlWriter xmlWriter, LsxXmlFormat.ModuleShortDesc moduleShortDesc)
    {
        xmlWriter.WriteStartElement("node");
        xmlWriter.WriteAttributeString("id", "ModuleShortDesc");

        WriteAttribute(xmlWriter,
            "Folder",
            "LSString",
            moduleShortDesc.Folder
        );
        WriteAttribute(xmlWriter,
            "MD5",
            "LSString",
            moduleShortDesc.Md5
        );
        WriteAttribute(xmlWriter,
            "Name",
            "LSString",
            moduleShortDesc.Name
        );
        WriteAttribute(xmlWriter,
            "PublishHandle",
            "uint64",
            moduleShortDesc.PublishHandle
        );
        WriteAttribute(xmlWriter,
            "UUID",
            "guid",
            moduleShortDesc.Uuid
        );
        WriteAttribute(xmlWriter,
            "Version64",
            "int64",
            moduleShortDesc.Version
        );

        xmlWriter.WriteEndElement();
    }

    private static void WriteAttribute(XmlWriter xmlWriter, string id, string type, string value)
    {
        xmlWriter.WriteStartElement("attribute");
        xmlWriter.WriteAttributeString("id", id);
        xmlWriter.WriteAttributeString("type", type);
        xmlWriter.WriteAttributeString("value", value);
        xmlWriter.WriteEndElement();
    }

#endregion private methods
}
