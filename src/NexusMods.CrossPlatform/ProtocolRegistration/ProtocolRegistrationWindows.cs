using System.Runtime.Versioning;
using Microsoft.Win32;

namespace NexusMods.CrossPlatform.ProtocolRegistration;

/// <summary>
/// protocol registration for windows
/// </summary>
[SupportedOSPlatform("windows")]
public class ProtocolRegistrationWindows : IProtocolRegistration
{
    /// <summary>
    /// constructor
    /// </summary>
    public ProtocolRegistrationWindows()
    {
    }

    /// <inheritdoc/>
    public Task<bool> IsSelfHandler(string protocol)
    {
        using var key = GetClassKey(protocol);
        using var commandKey = GetCommandKey(key);

        return Task.FromResult(((string?)commandKey.GetValue("") ?? "").Contains(GetOwnExe()));
    }

    /// <inheritdoc/>
    public Task<string?> Register(string protocol, string friendlyName, string workingDirectory, string commandLine)
    {
        using var key = GetClassKey(protocol);
        key.SetValue("", "URL:" + friendlyName);
        key.SetValue("URL Protocol", "");

        using var commandKey = GetCommandKey(key);

        var res = (string?)commandKey.GetValue("");
        commandKey.SetValue("", commandLine);
        commandKey.SetValue("WorkingDirectory", workingDirectory);

        return Task.FromResult(res);
    }

    /// <inheritdoc/>
    public Task<string?> RegisterSelf(string protocol)
    {
        var exePath = GetOwnExe();
        return Register(protocol, "NMA", Path.GetDirectoryName(exePath)!, exePath + " protocol-invoke --url \"%1\"");
    }

    private static string GetOwnExe()
    {
        return System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
    }

    private static RegistryKey GetClassKey(string protocol)
    {
        return Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + protocol);
    }

    private static RegistryKey GetCommandKey(RegistryKey parent)
    {
        return parent.CreateSubKey(@"shell\open\command");
    }
}
