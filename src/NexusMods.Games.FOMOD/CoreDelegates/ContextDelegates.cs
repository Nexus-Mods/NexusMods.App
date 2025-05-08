using FomodInstaller.Interface;
using Microsoft.Extensions.Logging;

namespace NexusMods.Games.FOMOD.CoreDelegates;

// TODO: This entire class needs to be implemented properly.
// Vortex: https://github.com/Nexus-Mods/Vortex/blob/b80d5b6d1b53e0dc6e0199c5371394261f8040b9/src/extensions/installer_fomod/delegates/Context.ts

public class ContextDelegates : IContextDelegates
{
    private readonly ILogger<ContextDelegates> _logger;

    public ContextDelegates(ILogger<ContextDelegates> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns the current version of the mod manager.
    /// </summary>
    public Task<string> GetAppVersion()
    {
        // Usage: https://github.com/Nexus-Mods/fomod-installer/blob/bfd8d4dee8e6e75c450d1b8bb5f66f243ab5d1ca/InstallScripting/XmlScript/ModManagerCondition.cs#L36-L50
        // NOTE(erri120): This is supposed to return the version of the mod manager. This is just beyond stupid.
        _logger.LogWarning($"NotImplemented: {nameof(GetAppVersion)}. Using default value: empty string");
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// Returns the current version of the game.
    /// </summary>
    public Task<string> GetCurrentGameVersion()
    {
        // Usage: https://github.com/Nexus-Mods/fomod-installer/blob/bfd8d4dee8e6e75c450d1b8bb5f66f243ab5d1ca/InstallScripting/XmlScript/GameVersionCondition.cs#L36-L54
        // NOTE(erri120): The returned string gets parsed as a System.Version and compared to a minimum version.
        _logger.LogWarning($"NotImplemented: {nameof(GetCurrentGameVersion)}. Using default value: empty string");
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// Returns the version of a script extender.
    /// </summary>
    /// <param name="extender">Script extender identifier.</param>
    public Task<string> GetExtenderVersion(string extender)
    {
        // Usage: https://github.com/Nexus-Mods/fomod-installer/blob/bfd8d4dee8e6e75c450d1b8bb5f66f243ab5d1ca/InstallScripting/XmlScript/SEVersionCondition.cs#L45
        // NOTE(erri120): The returned string gets parsed as a System.Version and compared to a minimum version.
        // The 'extender' parameter is an identifier with 4 letters like "fose", "nvse" or "skse".
        // Don't ask me why this isn't just an enum, I have no fucking idea.
        // Even Vortex doesn't have this implemented correctly: https://github.com/Nexus-Mods/Vortex/blob/b80d5b6d1b53e0dc6e0199c5371394261f8040b9/src/extensions/installer_fomod/delegates/Context.ts#L71-L82
        // They do have a list of extenders: https://github.com/Nexus-Mods/Vortex/blob/b80d5b6d1b53e0dc6e0199c5371394261f8040b9/src/extensions/installer_fomod/delegates/Context.ts#L21-L33

        _logger.LogWarning($"NotImplemented: {nameof(GetExtenderVersion)} with extender='{{Extender}}'. Using default value: empty string", extender);
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// Returns whether a script extender is present.
    /// </summary>
    public Task<bool> IsExtenderPresent()
    {
        // NOTE(erri120): This is just fucking stupid. GetExtenderVersion has a parameter for the specific extender
        // but this method doesn't. The Vortex implementation derives the extender from the current game using
        // this list: https://github.com/Nexus-Mods/Vortex/blob/b80d5b6d1b53e0dc6e0199c5371394261f8040b9/src/extensions/installer_fomod/delegates/Context.ts#L21-L33
        _logger.LogWarning($"NotImplemented: {nameof(IsExtenderPresent)}. Using default value: true");
        return Task.FromResult(true);
    }

    public Task<bool> CheckIfFileExists(string fileName)
    {
        _logger.LogWarning($"NotImplemented: {nameof(CheckIfFileExists)} with fileName='{{FileName}}'. Using default value: true", fileName);
        return Task.FromResult(true);
    }

    public Task<byte[]> GetExistingDataFile(string dataFile)
    {
        _logger.LogWarning($"NotImplemented: {nameof(GetExistingDataFile)} with dataFile='{{DataFile}}'. Using default value: null", dataFile);
        return Task.FromResult((byte[])null!);
    }

    public Task<string[]> GetExistingDataFileList(string folderPath, string searchFilter, bool isRecursive)
    {
        _logger.LogWarning($"NotImplemented: {nameof(GetExistingDataFileList)} with folderPath='{{FolderPath}}', searchFilter='{{SearchFilter}}', isRecursive={{IsRecursive}}. Using default value: null", folderPath, searchFilter, isRecursive);
        return Task.FromResult((string[])null!);
    }
}
