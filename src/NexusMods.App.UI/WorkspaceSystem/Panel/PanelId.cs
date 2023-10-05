using Vogen;

namespace NexusMods.App.UI.WorkspaceSystem;

[ValueObject<Guid>]
public readonly partial struct PanelId
{
    public static readonly PanelId Empty = From(Guid.Empty);
    public static PanelId New() => From(Guid.NewGuid());
}
