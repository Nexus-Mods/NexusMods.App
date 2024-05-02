using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.DataModel;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.App.Commandline;

/// <summary>
///     These verbs are used in cleaning up unneeded data related to the application.
///     Namely, Garbage Collection tasks.
/// </summary>
internal static class CleanupVerbs
{
    // ReSharper disable once UnusedMethodReturnValue.Global
    internal static IServiceCollection AddCleanupVerbs(this IServiceCollection services) =>
        services.AddVerb(() => UninstallApp);
    
    [Verb("uninstall-app", "Uninstall the application and revert games to their original state")]
    private static async Task<int> UninstallApp(
        [Injected] IRenderer renderer,
        [Option("t", "--text", "")] string? uwu,
        [Injected] ILoadoutRegistry loadoutRegistry,
        [Injected] ISettingsManager settingsManager,
        [Injected] IFileSystem fileSystem)
    {
        // Step 1: Revert the managed games to their original state
        var managedInstallations = loadoutRegistry.AllLoadouts()
            .Select(loadout => loadout.Installation)
            .Distinct();

        foreach (var installation in managedInstallations)
        {
            try
            {
                var synchronizer = installation.GetGame().Synchronizer;
                await synchronizer.UnManage(installation);
                await renderer.Text($"Reverted {installation.Game.Name} to its original state");
            }
            catch (Exception ex)
            {
                await renderer.Error(ex, "Error reverting {0}: {1}", installation.Game.Name, ex.Message);
            }
        }

        // Step 2: Delete application-specific directories
        try
        {
            /*
                Note (Sewer): There's a possibility some data may be left behind if
                the user manually modified the logging configuration to point
                their logs outside of the regular 'Logs' folder.
                
                Specifically, the historical logs may use Layout Renderers
                https://nlog-project.org/config/?tab=layout-renderers that
                are other than {##}. Resolving these on our end would be hard.
            */
            var dataModelSettings = settingsManager.Get<DataModelSettings>();
            var fileExtractorSettings = settingsManager.Get<FileExtractorSettings>();
            
            // TODO: LoggingSettings can't be set up via DI right now, and are not
            // configurable as of the time of writing this code.
            var loggingSettings = LoggingSettings.CreateDefault(fileSystem.OS);
            var appFiles = (AbsolutePath[]) [ 
                dataModelSettings.DataStoreFilePath.ToPath(fileSystem),
                loggingSettings.MainProcessLogFilePath.ToPath(fileSystem),
                loggingSettings.SlimProcessLogFilePath.ToPath(fileSystem),
            ];

            var appDirectories = new[]
            {
                dataModelSettings.MnemonicDBPath.ToPath(fileSystem),
                fileExtractorSettings.TempFolderLocation.ToPath(fileSystem),
                LoggingSettings.GetLogBaseFolder(OSInformation.Shared, fileSystem)
            }.Concat(dataModelSettings.ArchiveLocations.Select(path => path.ToPath(fileSystem)));

            if (fileSystem.OS.IsUnix())
                await DeleteRemainingFilesUnix(renderer, appFiles, appDirectories);
            else
                DeleteRemainingFilesWindows(renderer, appFiles, appDirectories);

            await renderer.Text("Application uninstalled successfully");
            return 0;
        }
        catch (Exception ex)
        {
            await renderer.Error(ex, $"Error deleting application directories: {ex.Message}");
            return -1;
        }
    }

    private static async Task DeleteRemainingFilesUnix(IRenderer renderer, AbsolutePath[] appFiles, IEnumerable<AbsolutePath> appDirectories)
    {
        await DeleteFilesUnix(renderer, appFiles);
        await DeleteDirectoriesUnix(renderer, appDirectories);
    }
    
    private static void DeleteRemainingFilesWindows(IRenderer renderer, AbsolutePath[] appFiles, IEnumerable<AbsolutePath> appDirectories)
    {
        // Generate the batch script content
        var scriptContent = new StringBuilder();
        scriptContent.AppendLine("@echo off");

        // Kill all processes with the name "NexusMods.App"
        // Note(sewer) Technically we can start with `dotnet NexusMods.App`,
        // but this will only really ever get called from the setup binary,
        // so we have control over this.
        scriptContent.AppendLine("taskkill /F /IM \"NexusMods.App.exe\" /T");

        // Delete application-specific files
        foreach (var appFile in appFiles)
            scriptContent.AppendLine($"del /Q \"{appFile}\"");

        // Delete application-specific directories
        foreach (var directory in appDirectories)
            scriptContent.AppendLine($"rmdir /S /Q \"{directory}\"");

        // Save the script to a temporary file
        var tempScriptPath = Path.GetTempFileName().Replace(".tmp", ".bat");
        File.WriteAllText(tempScriptPath, scriptContent.ToString());

        // Execute the script using cmd.exe
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/C \"{tempScriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (var process = new Process())
        {
            process.StartInfo = processStartInfo;
            process.Start();
            process.WaitForExit();
        }

        // In case somehow our script does not die.
        // Delete the temporary script file
        File.Delete(tempScriptPath);
    }

    private static async Task DeleteDirectoriesUnix(IRenderer renderer, IEnumerable<AbsolutePath> appDirectories)
    {
        foreach (var directory in appDirectories)
        {
            if (!directory.DirectoryExists())
                continue;

            directory.Delete();
            await renderer.Text("Deleted directory: {0}", directory);
        }
    }

    private static async Task DeleteFilesUnix(IRenderer renderer, AbsolutePath[] appFiles)
    {
        foreach (var appFile in appFiles)
        {
            if (!appFile.FileExists)
                continue;

            try
            {
                appFile.Delete();
                await renderer.Text("Deleted file: {0}", appFile);
            }
            catch (Exception e)
            {
                await renderer.Error(e, "Failed to delete file: {0}", appFile); 
            }
        }
    }
}
