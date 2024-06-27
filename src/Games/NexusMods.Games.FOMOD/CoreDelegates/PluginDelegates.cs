using FomodInstaller.Interface;
using Microsoft.Extensions.Logging;

namespace NexusMods.Games.FOMOD.CoreDelegates;

// TODO: This entire class needs to be implemented properly.
// See this for usages: https://github.com/Nexus-Mods/fomod-installer/blob/bfd8d4dee8e6e75c450d1b8bb5f66f243ab5d1ca/InstallScripting/XmlScript/PluginCondition.cs#L99-L105
// Vortex: https://github.com/Nexus-Mods/Vortex/blob/b80d5b6d1b53e0dc6e0199c5371394261f8040b9/src/extensions/installer_fomod/delegates/Plugins.ts

public class PluginDelegates : IPluginDelegates
{
    private readonly ILogger<PluginDelegates> _logger;

    public PluginDelegates(ILogger<PluginDelegates> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns an array of all plugins filtered using <paramref name="activeOnly"/>.
    /// </summary>
    public Task<string[]> GetAll(bool activeOnly)
    {
        _logger.LogWarning($"NotImplemented: {nameof(GetAll)} with activeOnly={{ActiveOnly}}. Using default value: empty array.", activeOnly);
        return Task.FromResult(Array.Empty<string>());
    }

    /// <summary>
    /// Checks whether or not the plugin at <paramref name="pluginName"/> is active or not.
    /// </summary>
    public Task<bool> IsActive(string pluginName)
    {
        _logger.LogWarning($"NotImplemented: {nameof(IsActive)} with pluginName='{{PluginName}}'. Using default value: true", pluginName);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Checks whether or not the plugin file at <paramref name="pluginName"/> exists.
    /// </summary>
    public Task<bool> IsPresent(string pluginName)
    {
        _logger.LogWarning($"NotImplemented: {nameof(IsPresent)} with pluginName='{{PluginName}}'. Using default value: true", pluginName);
        return Task.FromResult(true);
    }
}
