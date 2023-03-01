using Microsoft.Win32;
using System.Runtime.Versioning;

namespace NexusMods.Common;

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
    public bool IsSelfHandler(string protocol)
    {
        using var key = GetClassKey(protocol);
        using var commandKey = GetCommandKey(key);

        return ((string?)commandKey.GetValue("") ?? "").Contains(GetOwnExe());
    }

    /// <inheritdoc/>
    public string Register(string protocol, string friendlyName, string? commandLine = null)
    {
        using var key = GetClassKey(protocol);
        key.SetValue("", "URL:" + friendlyName);
        key.SetValue("URL Protocol", "");

        using var commandKey = GetCommandKey(key);

        string res = (string)(commandKey.GetValue("") ?? "");
        if (commandLine != null)
        {
            commandKey.SetValue("", commandLine);
        }
        else
        {
            commandKey.DeleteValue("");
        }

        return res;
    }

    /// <inheritdoc/>
    public string RegisterSelf(string protocol)
    {
        return Register(protocol, "NMA", GetOwnExe() + " protocol-invoke --url \"%1\"");
    }

    private string GetOwnExe()
    {
        return System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
    }

    private RegistryKey GetClassKey(string protocol)
    {
        return Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + protocol);
    }

    private RegistryKey GetCommandKey(RegistryKey parent)
    {
        return parent.CreateSubKey(@"shell\open\command");
    }
}
