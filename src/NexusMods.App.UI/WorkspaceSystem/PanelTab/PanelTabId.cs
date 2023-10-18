using Vogen;

namespace NexusMods.App.UI.WorkspaceSystem;

[ValueObject<Guid>]
public readonly partial struct PanelTabId
{
    public static readonly PanelTabId Empty = From(Guid.Empty);
}
