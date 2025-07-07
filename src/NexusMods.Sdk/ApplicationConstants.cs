using System.Reflection;
using JetBrains.Annotations;

namespace NexusMods.Sdk;

/// <summary>
/// Static values that are constant for the entire lifetime of the application.
/// </summary>
/// <remarks>
/// Values can be supplied via preprocessor symbols or environment variables.
/// </remarks>
[PublicAPI]
public static class ApplicationConstants
{
    /// <summary>
    /// Whether the application is running in debug mode.
    /// </summary>
    public static readonly bool IsDebug;

    /// <summary>
    /// Whether the application is running in a CI environment.
    /// </summary>
    public static readonly bool IsCI;

    /// <summary>
    /// The version of the commit the app was build from.
    /// </summary>
    public static readonly Version Version;

    /// <summary>
    /// The hash of the commit that the app was build from.
    /// </summary>
    public static readonly string? CommitHash;

    /// <summary>
    /// The default user agent of the application.
    /// </summary>
    public static readonly UserAgent UserAgent;

    /// <summary>
    /// The installation method.
    /// </summary>
    public static readonly InstallationMethod InstallationMethod;

    static ApplicationConstants()
    {
        if (EnvironmentVariables.TryGetBoolean(EnvironmentVariableNames.IsDebug, out var isDebug))
        {
            IsDebug = isDebug;
        }
        else
        {
            IsDebug =
#if DEBUG
                true;
#else
                false;
#endif
        }

        if (EnvironmentVariables.TryGetBoolean(name: "CI", out var isCI))
        {
            IsCI = isCI;
        }

        var currentAssembly = typeof(ApplicationConstants).Assembly;

        try
        {
            // This attribute is set by SourceLink (https://github.com/dotnet/sourcelink)
            var attribute = Attribute.GetCustomAttribute(currentAssembly, typeof(AssemblyInformationalVersionAttribute));
            if (attribute is AssemblyInformationalVersionAttribute assemblyInformationalVersionAttribute)
            {
                var informationalVersion = assemblyInformationalVersionAttribute.InformationalVersion;
                var sha = GetSha(informationalVersion);
                CommitHash = sha;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        if (IsDebug)
        {
            Version = FallbackVersion;
        }
        else
        {
            if (EnvironmentVariables.TryGetString(EnvironmentVariableNames.Version, out var sVersion) &&
                Version.TryParse(sVersion, out var version))
            {
                Version = version;
            }
            else
            {
                try
                {
                    Version = currentAssembly.GetName().Version ?? FallbackVersion;
                }
                catch (Exception)
                {
                    Version = FallbackVersion;
                }
            }
        }

        if (!EnvironmentVariables.TryGetString(EnvironmentVariableNames.UserAgentName, out var userAgentName))
            userAgentName = DefaultUserAgentName;

        UserAgent = new UserAgent(userAgentName, ApplicationVersion: Version.ToSafeString(maxFieldCount: 3));

        if (EnvironmentVariables.TryGetEnum<InstallationMethod>(EnvironmentVariableNames.InstallationMethod, out var installationMethod))
        {
            InstallationMethod = installationMethod;
        }
        else
        {
            InstallationMethod =
#if INSTALLATION_METHOD_ARCHIVE
                InstallationMethod.Archive;
#elif INSTALLATION_METHOD_APPIMAGE
                InstallationMethod.AppImage;
#elif INSTALLATION_METHOD_PACKAGE_MANAGER
                InstallationMethod.PackageManager;
#elif INSTALLATION_METHOD_INNO_SETUP
                InstallationMethod.InnoSetup;
#elif INSTALLATION_METHOD_FLATPAK
                InstallationMethod.Flatpak;
#else
                InstallationMethod.Manually;
#endif
        }
    }

    private static string? GetSha(ReadOnlySpan<char> input)
    {
        var plusIndex = input.IndexOf('+');
        if (plusIndex == -1 || plusIndex + 1 >= input.Length) return null;
        return input[(plusIndex + 1)..].ToString();
    }

#region Default values

    /// <summary>
    /// Fallback version used for debug builds or release builds with no version.
    /// </summary>
    private static readonly Version FallbackVersion = new(0, 0, 1);

    /// <summary>
    /// Default user agent application name.
    /// </summary>
    private const string DefaultUserAgentName = "NexusModsApp";

#endregion
}
