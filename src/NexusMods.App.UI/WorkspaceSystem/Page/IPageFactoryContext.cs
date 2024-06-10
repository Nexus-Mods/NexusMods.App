namespace NexusMods.App.UI.WorkspaceSystem;

/// <summary>
/// Interface for contexts used by the <see cref="IPageFactory"/>.
/// </summary>
public interface IPageFactoryContext
{
    /// <summary>
    /// Gets whether the context is ephemeral in nature and should not be
    /// persisted.
    /// </summary>
    bool IsEphemeral => false;
}
