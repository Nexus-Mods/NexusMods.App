using System.Reflection;
using JetBrains.Annotations;

namespace NexusMods.App.BuildInfo;

/// <summary>
/// Constants supplied during runtime.
/// </summary>
[PublicAPI]
public static class ApplicationConstants
{
    static ApplicationConstants()
    {
        try
        {
            // This attribute is set by SourceLink (https://github.com/dotnet/sourcelink)
            var attribute = Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyInformationalVersionAttribute));
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

        var debugVersion = new Version(0, 0, 1);
        if (CompileConstants.IsDebug)
        {
            Version = debugVersion;
        }
        else
        {
            try
            {
                Version = Assembly.GetExecutingAssembly().GetName().Version ?? debugVersion;
            }
            catch (Exception)
            {
                Version = debugVersion;
            }
        }

        try
        {
            var environmentVariable = Environment.GetEnvironmentVariable(InstallationMethodEnvironmentVariableName);
            if (environmentVariable is not null && Enum.TryParse(environmentVariable, ignoreCase: true, out InstallationMethod installationMethod))
            {
                InstallationMethod = installationMethod;
            }
            else
            {
                InstallationMethod = CompileConstants.InstallationMethod;
            }
        }
        catch (Exception)
        {
            InstallationMethod = CompileConstants.InstallationMethod;
        }

        UserAgent = $"NexusModsApp/{Version.ToString(fieldCount: 3)}";
    }

    private static string? GetSha(string input)
    {
        var span = input.AsSpan();
        var plusIndex = span.IndexOf('+');
        return plusIndex == -1 ? null : span[(plusIndex + 1)..].ToString();
    }

    /// <summary>
    /// Gets the current Version.
    /// </summary>
    public static Version Version { get; }

    /// <summary>
    /// Gets the hash of the current commit.
    /// </summary>
    public static string? CommitHash { get; }

    /// <summary>
    /// Gets the default user-agent.
    /// </summary>
    public static string UserAgent { get; }

    /// <summary>
    /// Gets the installation method.
    /// </summary>
    /// <remarks>
    /// This differs from <see cref="CompileConstants.InstallationMethod"/> in that the
    /// value can be overwritten using the environment variable <see cref="InstallationMethodEnvironmentVariableName"/>.
    /// </remarks>
    public static InstallationMethod InstallationMethod { get; }

    /// <summary>
    /// Environment variable name to overwrite <see cref="InstallationMethod"/>.
    /// </summary>
    public const string InstallationMethodEnvironmentVariableName = "NEXUS_MODS_APP_INSTALLATION_METHOD";
}

