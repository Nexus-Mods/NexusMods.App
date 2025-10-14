using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace NexusMods.Backend.OS;

internal partial class WindowsInterop
{
    [SupportedOSPlatformGuard("windows")]
    private bool IsWindows() => _os.IsWindows;

    public ValueTask RegisterUriSchemeHandler(string scheme, bool setAsDefaultHandler = true, CancellationToken cancellationToken = default)
    {
        if (!IsWindows()) return ValueTask.CompletedTask;

        // NOTE(erri120): See this comment for an in-depth guide on protocol handlers:
        // https://github.com/Nexus-Mods/NexusMods.App/pull/1691#issuecomment-2194418849
        // We've decided use the same method that Vortex and MO2 use, which is using a
        // generic ProgID. This means we're always overwriting the existing values.

        try
        {
            SetAsDefaultHandler(scheme);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while updating registry to register protocol handler for `{Scheme}`", scheme);
        }

        return ValueTask.CompletedTask;
    }

    private static string CreateProgId(string uriScheme) => $"NexusMods.App.{uriScheme}";

    [SupportedOSPlatform("windows")]
    private void RegisterApplication(string uriScheme)
    {
        // https://learn.microsoft.com/en-us/windows/win32/shell/default-programs

        const string capabilitiesPath = @"SOFTWARE\Nexus Mods\NexusMods.App\Capabilities";

        using var key = Registry.CurrentUser.CreateSubKey(capabilitiesPath);
        key.SetValue("ApplicationName", "Nexus Mods App");
        key.SetValue("ApplicationDescription", "Mod Manager for your games");

        using var urlAssociationsKey = key.CreateSubKey("UrlAssociations");
        urlAssociationsKey.SetValue(uriScheme, CreateProgId(uriScheme));

        using var registeredApplicationsKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\RegisteredApplications");
        registeredApplicationsKey.SetValue("NexusMods.App", capabilitiesPath);

        CreateProgIdClass(CreateProgId(uriScheme), $"Nexus Mods App {uriScheme.ToUpperInvariant()} Handler", isProtocolHandler: false);
    }

    [SupportedOSPlatform("windows")]
    private void CreateProgIdClass(string progId, string name, bool isProtocolHandler)
    {
        // https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/platform-apis/aa767914(v=vs.85)

        using var key = Registry.CurrentUser.CreateSubKey(@$"SOFTWARE\Classes\{progId}");
        key.SetValue("", name);
        if (isProtocolHandler) key.SetValue("URL Protocol", "");

        using var commandKey = key.CreateSubKey(@"shell\open\command");

        var executable = GetRunningExecutablePath(out _);
        commandKey.SetValue("", $"\"{executable.ToNativeSeparators(_fileSystem.OS)}\" \"%1\"");

        // NOTE(erri120): can't set the working directory for generic protocol handlers
        // due to possible issues with Vortex/MO2.
        if (!isProtocolHandler) commandKey.SetValue("WorkingDirectory", $"\"{executable.Parent.ToNativeSeparators(_fileSystem.OS)}\"");
    }

    [SupportedOSPlatform("windows")]
    private void SetAsDefaultHandler(string uriScheme)
    {
        CreateProgIdClass(uriScheme, $"Nexus Mods App {uriScheme.ToUpperInvariant()} Handler", isProtocolHandler: true);
    }
}
