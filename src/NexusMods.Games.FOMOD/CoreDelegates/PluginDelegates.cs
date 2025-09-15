using FomodInstaller.Interface;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Games.FOMOD.CoreDelegates;

// TODO: This entire class needs to be implemented properly.
// See this for usages: https://github.com/Nexus-Mods/fomod-installer/blob/bfd8d4dee8e6e75c450d1b8bb5f66f243ab5d1ca/InstallScripting/XmlScript/PluginCondition.cs#L99-L105
// Vortex: https://github.com/Nexus-Mods/Vortex/blob/b80d5b6d1b53e0dc6e0199c5371394261f8040b9/src/extensions/installer_fomod/delegates/Plugins.ts

public class PluginDelegates : IPluginDelegates
{
    private readonly ILogger<PluginDelegates> _logger;
    private readonly InstallerDelegates _installerDelegates;
    public PluginDelegates(ILogger<PluginDelegates> logger, InstallerDelegates installerDelegates)
    {
        _logger = logger;
        _installerDelegates = installerDelegates;
    }

    /// <summary>
    /// Returns an array of all plugins filtered using <paramref name="activeOnly"/>.
    /// </summary>
    public Task<string[]> GetAll(bool activeOnly)
    {
        Loadout.ReadOnly loadout = _installerDelegates.CurLoadout;
        List<string> loadoutItems = new List<string>();
        foreach (var item in loadout.Items)
        {
            if (!activeOnly)
            {
                loadoutItems.Add(item.Name);
            }
            else
            {
                if (!item.IsDisabled)
                {
                    loadoutItems.Add(item.Name);
                }
            }
        }
        
        return Task.FromResult(loadoutItems.ToArray());
    }

    /// <summary>
    /// Checks whether or not the plugin at <paramref name="pluginName"/> is active or not.
    /// </summary>
    public Task<bool> IsActive(string pluginName)
    {
        Loadout.ReadOnly loadout = _installerDelegates.CurLoadout;
        foreach (LoadoutItem.ReadOnly item in loadout.Items)
        {
            if (item.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(!item.IsDisabled);
            }
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Checks whether or not the plugin file at <paramref name="pluginName"/> exists.
    /// </summary>
    public Task<bool> IsPresent(string pluginName)
    {
        Loadout.ReadOnly loadout = _installerDelegates.CurLoadout;
        foreach (LoadoutItem.ReadOnly item in loadout.Items)
        {
            if (item.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }
}
