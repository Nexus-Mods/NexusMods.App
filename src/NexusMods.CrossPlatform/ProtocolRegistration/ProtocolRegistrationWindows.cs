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
            UpdateRegistry(uriScheme);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while updating registry to register protocol handler for `{Scheme}`", uriScheme);
        }

        if (setAsDefaultHandler)
        {
            // On Windows, this is done via the "UserChoice" registry key.
            // We can't set this automatically without much hassle. See this for details:
            // https://www.winhelponline.com/blog/set-default-browser-file-associations-command-line-windows-10/
            _logger.LogDebug("Skipping setting default handler for `{Scheme}`", uriScheme);
        }

        return Task.CompletedTask;
    }

    private void UpdateRegistry(string uriScheme)
    {
        // https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/platform-apis/aa767914(v=vs.85)

        using var key = GetClassKey(uriScheme);
        key.SetValue("", "URL:Nexus Mods App");
        key.SetValue("URL Protocol", "");

        using var commandKey = GetCommandKey(key);

        var executable = _osInterop.GetOwnExe();

        commandKey.SetValue("", $"\"{executable.ToNativeSeparators(_fileSystem.OS)}\" \"%1\"");
        commandKey.SetValue("WorkingDirectory", $"\"{executable.Parent.ToNativeSeparators(_fileSystem.OS)}\"");
    }

    private static RegistryKey GetClassKey(string protocol)
    {
        return Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\" + protocol);
    }

    private static RegistryKey GetCommandKey(RegistryKey parent)
    {
        return parent.CreateSubKey(@"shell\open\command");
    }
}
