using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CliWrap;

namespace NexusMods.CrossPlatform.Process;

internal class XDGOpenDependency : ExecutableRuntimeDependency
{
    public override string DisplayName => "xdg-open";
    public override string Description => "Open a URL in the user's preferred application that handles the respective URL or file type.";
    public override Uri Homepage { get; } = new("https://www.freedesktop.org/wiki/Software/xdg-utils/");
    public override OSPlatform[] SupportedPlatforms { get; } = [OSPlatform.Linux];

    public XDGOpenDependency(IProcessFactory processFactory) : base(processFactory) { }

    public Command CreateOpenUriCommand(Uri uri)
    {
        return Cli.Wrap("xdg-open").WithArguments(new[] { uri.ToString() }, escape: true);
    }

    protected override Command BuildQueryCommand(PipeTarget outpuPipeTarget)
    {
        var command = Cli.Wrap("xdg-open").WithArguments("--version").WithStandardOutputPipe(outpuPipeTarget);
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
        // expected: "xdg-open 1.2.1\n"
        const string prefix = "xdg-open ";

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
