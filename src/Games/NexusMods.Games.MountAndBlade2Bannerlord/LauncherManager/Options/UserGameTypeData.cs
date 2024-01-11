using System.Xml;
using System.Xml.Serialization;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Options;

public sealed class UserGameTypeData
{
    public static UserGameTypeData Empty => new() { ModDatas = new() };

    public required List<UserModData> ModDatas { get; set; }

    [XmlAnyElement]
    [XmlText]
    public XmlNode[] Nodes { get; set; } = Array.Empty<XmlNode>();
}
