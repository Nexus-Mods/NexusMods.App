using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CliWrap;

namespace NexusMods.CrossPlatform.Process;

internal class XDGSettingsDependency : ExecutableRuntimeDependency
{
    public override string DisplayName => "xdg-settings";
    public override string Description => "Get or set the default URL handlers.";
    public override Uri Homepage { get; } = new("https://www.freedesktop.org/wiki/Software/xdg-utils/");
    public override OSPlatform[] SupportedPlatforms { get; } = [OSPlatform.Linux];

    public XDGSettingsDependency(IProcessFactory processFactory) : base(processFactory) { }

    public Command CreateSetDefaultUrlSchemeHandlerCommand(string uriScheme, string desktopFile)
    {
        var command = Cli
            .Wrap("xdg-settings")
            .WithArguments($"set default-url-scheme-handler {uriScheme} {desktopFile}")
            .WithValidation(CommandResultValidation.None);

        return command;
    }

    protected override Command BuildQueryCommand(PipeTarget outpuPipeTarget)
    {
        var command = Cli.Wrap("xdg-settings").WithArguments("--version").WithStandardOutputPipe(outpuPipeTarget);
        return command;
    }

    protected override RuntimeDependencyInformation ToInformation(ReadOnlySpan<char> output)
    {
        if (TryParseVersion(output, out var rawVersion, out var version))
        {
            return new RuntimeDependencyInformation
            {
                RawVersion = rawVersion,
                Version = version,
            };
        }

        return new RuntimeDependencyInformation();
    }

    internal static bool TryParseVersion(
        ReadOnlySpan<char> output,
        [NotNullWhen(true)] out string? rawVersion,
        out Version? version)
    {
        // expected: "xdg-settings 1.2.1\n"
        const string prefix = "xdg-settings ";

        rawVersion = null;
        version = null;

        if (!output.StartsWith(prefix, StringComparison.Ordinal)) return false;
        if (output.Length < prefix.Length + 2) return false;

        var span = output[prefix.Length..];
        var index = span.IndexOf('\n');

        if (index != -1)
        {
            span = span[..index];
        }

        rawVersion = span.ToString();
        _ = Version.TryParse(rawVersion, out version);

        return true;
    }
}
