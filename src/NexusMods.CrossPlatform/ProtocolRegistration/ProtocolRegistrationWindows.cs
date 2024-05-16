using System.Runtime.Versioning;
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
    private readonly IOSInterop _osInterop;

    /// <summary>
    /// constructor
    /// </summary>
    public ProtocolRegistrationWindows(IOSInterop osInterop)
    {
        _osInterop = osInterop;
    }

    /// <inheritdoc/>
    public Task<bool> IsSelfHandler(string protocol)
    {
        using var key = GetClassKey(protocol);
        using var commandKey = GetCommandKey(key);

        return Task.FromResult(((string?)commandKey.GetValue("") ?? "").Contains(_osInterop.GetOwnExe().ToString()));
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
        var exePath = _osInterop.GetOwnExe();
        var osInfo = FileSystem.Shared.OS;
        return Register(protocol, "NMA", "\""+exePath.Parent.ToNativeSeparators(osInfo) + "\"", 
            "\""+exePath.ToNativeSeparators(osInfo)+"\"" +
            " protocol-invoke --url \"%1\"");
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
