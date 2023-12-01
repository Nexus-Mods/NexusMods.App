using System.Xml;
using System.Xml.Serialization;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Options;

public sealed class UserModData
{
    public required string Id { get; set; }
    public required bool IsSelected { get; set; }

    [XmlAnyElement]
    [XmlText]
    public XmlNode[] Nodes { get; set; } = Array.Empty<XmlNode>();
}
