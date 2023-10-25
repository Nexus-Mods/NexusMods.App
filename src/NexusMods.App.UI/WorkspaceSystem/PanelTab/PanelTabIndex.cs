using Vogen;

namespace NexusMods.App.UI.WorkspaceSystem;

[ValueObject<uint>]
public readonly partial struct PanelTabIndex
{
    public static readonly PanelTabIndex Max = From(uint.MaxValue);
}
