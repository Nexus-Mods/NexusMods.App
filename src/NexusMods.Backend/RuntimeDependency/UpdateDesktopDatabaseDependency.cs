using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CliWrap;
using NexusMods.Sdk;

namespace NexusMods.Backend.RuntimeDependency;

internal class UpdateDesktopDatabaseDependency : ExecutableRuntimeDependency
{
    public override string DisplayName => "update-desktop-database";
    public override string Description => "Updates the database containing a cache of MIME types handled by desktop files.";
    public override Uri Homepage { get; } = new("https://www.freedesktop.org/wiki/Software/desktop-file-utils/");
    public override OSPlatform[] SupportedPlatforms { get; } = [OSPlatform.Linux];

    public UpdateDesktopDatabaseDependency(IProcessRunner runner) : base(runner) { }

    public Command BuildUpdateCommand(string applicationsDirectory)
    {
        return Cli
            .Wrap("update-desktop-database")
            .WithArguments(applicationsDirectory);
    }

    protected override Command BuildQueryCommand(PipeTarget outpuPipeTarget)
    {
        var command = Cli.Wrap("update-desktop-database").WithArguments("--version").WithStandardOutputPipe(outpuPipeTarget);
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
        // expected: "update-desktop-database 0.27\n"
        const string prefix = "update-desktop-database ";

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
