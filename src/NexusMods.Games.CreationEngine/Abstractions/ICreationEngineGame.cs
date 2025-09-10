namespace NexusMods.Games.CreationEngine.Abstractions;

public interface ICreationEngineGame
{
    /// <summary>
    /// Get the plugin utilities for this game.
    /// </summary>
    public IPluginUtilities PluginUtilities { get; }
}
