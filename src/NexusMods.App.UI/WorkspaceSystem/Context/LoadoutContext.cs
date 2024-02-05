using NexusMods.Abstractions.Loadouts;

namespace NexusMods.App.UI.WorkspaceSystem;

public record LoadoutContext(LoadoutId LoadoutId) : IWorkspaceContext;
