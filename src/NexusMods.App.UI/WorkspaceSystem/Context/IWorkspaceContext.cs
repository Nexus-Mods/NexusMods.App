using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

/// <summary>
/// Represents a workspace context.
/// </summary>
[PublicAPI]
public interface IWorkspaceContext
{
    /// <summary>
    /// Returns true if the current workspace context is still valid, meaning
    /// the data inside the context is still available and isn't referring to
    /// missing or deleted data.
    /// </summary>
    public bool IsValid(IServiceProvider serviceProvider);
}
