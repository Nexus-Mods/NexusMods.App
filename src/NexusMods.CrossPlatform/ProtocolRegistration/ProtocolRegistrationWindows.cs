using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.ProtocolRegistration;

/// <summary>
/// protocol registration for windows
/// </summary>
[SupportedOSPlatform("windows")]
public class ProtocolRegistrationWindows : IProtocolRegistration
{
    private readonly ILogger _logger;
    private readonly IOSInterop _osInterop;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ProtocolRegistrationWindows(
        ILogger<ProtocolRegistrationWindows> logger,
        IOSInterop osInterop,
        IFileSystem fileSystem)
    {
        _logger = logger;
        _osInterop = osInterop;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public Task RegisterHandler(string uriScheme, bool setAsDefaultHandler = true, CancellationToken cancellationToken = default)
    {
        try
        {
            RegisterApplication(uriScheme);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while updating registry to register protocol handler for `{Scheme}`", uriScheme);
        }

        if (setAsDefaultHandler)
        {
            try
            {
                SetAsDefaultHandler(uriScheme);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while registering as default protocol handler for `{Scheme}`", uriScheme);
            }
        }

        return Task.CompletedTask;
    }

    private void RegisterApplication(string uriScheme)
    {
        const string capabilitiesPath = @"SOFTWARE\Nexus Mods\NexusMods.App\Capabilities";

        using var key = Registry.CurrentUser.CreateSubKey(capabilitiesPath);
        key.SetValue("ApplicationName", "Nexus Mods App");
        key.SetValue("ApplicationDescription", "Mod Manager for your games");

        using var urlAssociationsKey = key.CreateSubKey("UrlAssociations");
        urlAssociationsKey.SetValue(uriScheme, uriScheme);

        using var registeredApplicationsKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\RegisteredApplications");
        registeredApplicationsKey.SetValue("NexusMods.App", capabilitiesPath);
    }

    private void SetAsDefaultHandler(string uriScheme)
    {
        // https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/platform-apis/aa767914(v=vs.85)

        using var key = Registry.CurrentUser.CreateSubKey(@$"SOFTWARE\Classes\{uriScheme}");
        key.SetValue("", "URL:Nexus Mods App");
        key.SetValue("URL Protocol", "");

        using var commandKey = key.CreateSubKey(@"shell\open\command");

        var executable = _osInterop.GetOwnExe();

        commandKey.SetValue("", $"\"{executable.ToNativeSeparators(_fileSystem.OS)}\" \"%1\"");
        commandKey.SetValue("WorkingDirectory", $"\"{executable.Parent.ToNativeSeparators(_fileSystem.OS)}\"");
    }
}
