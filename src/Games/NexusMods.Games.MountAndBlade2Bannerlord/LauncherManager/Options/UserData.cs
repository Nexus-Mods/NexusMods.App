using System.Xml;
using System.Xml.Serialization;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Options;

public sealed class UserData
{
    public required GameType GameType { get; set; }
    public required UserGameTypeData SingleplayerData { get; set; }
    public required UserGameTypeData MultiplayerData { get; set; }

    public bool FixCommonIssues { get; set; }
    public bool BetaSorting { get; set; }
    public bool DisableBinaryCheck { get; set; }
    public bool DisableCrashHandlerWhenDebuggerIsAttached { get; set; }
    public bool DisableCatchAutoGenExceptions { get; set; }
    public bool UseVanillaCrashHandler { get; set; }

    [XmlAnyElement]
    [XmlText]
    public XmlNode[] Nodes { get; set; } = Array.Empty<XmlNode>();
}
