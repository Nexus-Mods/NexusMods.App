using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using CliWrap;
using LinuxDesktopUtils.XDGDesktopPortal;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Process;

/// <summary>
/// OS interoperation for linux
/// </summary>
[SupportedOSPlatform("linux")]
internal class OSInteropLinux : AOSInterop
{
    private readonly IFileSystem _fileSystem;
    private readonly DesktopPortalConnectionManagerWrapper _portalWrapper;
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public OSInteropLinux(
        ILoggerFactory loggerFactory,
        DesktopPortalConnectionManagerWrapper portalWrapper,
        IProcessFactory processFactory,
        IFileSystem fileSystem) : base(fileSystem, loggerFactory, processFactory)
    {
        _fileSystem = fileSystem;
        _portalWrapper = portalWrapper;
        _logger = loggerFactory.CreateLogger<OSInteropLinux>();
    }

    /// <inheritdoc/>
    public override async Task OpenUrl(Uri url, bool logOutput = false, bool fireAndForget = false, CancellationToken cancellationToken = default)
    {
        var portal = await GetPortal();
        await portal.OpenUriAsync(url, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task OpenFile(AbsolutePath filePath, bool logOutput = false, bool fireAndForget = false, CancellationToken cancellationToken = default)
    {
        var portal = await GetPortal();
        await portal.OpenFileAsync(
            file: FilePath.From(filePath.ToNativeSeparators(_fileSystem.OS)),
            cancellationToken: cancellationToken
        );
    }

    public override async Task OpenFileInDirectory(AbsolutePath filePath, bool logOutput = false, bool fireAndForget = true, CancellationToken cancellationToken = default)
    {
        var portal = await GetPortal();
        await portal.OpenFileInDirectoryAsync(
            file: FilePath.From(filePath.ToNativeSeparators(_fileSystem.OS)),
            cancellationToken: cancellationToken
        );
    }

    private async ValueTask<OpenUriPortal> GetPortal()
    {
        var connectionManager = await _portalWrapper.GetInstance();
        var portal = await connectionManager.GetOpenUriPortalAsync();
        return portal;
    }

    /// <inheritdoc/>
    protected override Command CreateCommand(Uri uri) => throw new UnreachableException("Should never be called");

    /// <inheritdoc />
    public override AbsolutePath GetOwnExe()
    {
        // https://docs.appimage.org/packaging-guide/environment-variables.html#type-2-appimage-runtime
        // APPIMAGE: (Absolute) path to AppImage file (with symlinks resolved)
        var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE", EnvironmentVariableTarget.Process);
        if (appImagePath is null) return base.GetOwnExe();

        return _fileSystem.FromUnsanitizedFullPath(appImagePath);
    }

    /// <inheritdoc />
    public override async ValueTask<IReadOnlyList<FileSystemMount>> GetFileSystemMounts(CancellationToken cancellationToken = default)
    {
        var stdOut = new StringBuilder();
        var command = Cli.Wrap("df").WithArguments([
            "--local",                                          // limit listing to local file systems
            "--output=source,fstype,size,avail,target",         // output these columns
            "--block-size=1K",                                  // scale sizes
            "--exclude-type=tmpfs",                             // limit listing to file systems not of this type
            "--exclude-type=devtmpfs",
            "--exclude-type=vfat",
            "--exclude-type=efivarfs",
        ]).WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut));

        await ProcessFactory.ExecuteAsync(command, cancellationToken: cancellationToken);
        return ParseFileSystemMounts(_fileSystem, stdOut.ToString());
    }

    /// <inheritdoc />
    public override async ValueTask<FileSystemMount?> GetFileSystemMount(AbsolutePath path, IReadOnlyList<FileSystemMount> knownFileSystemMounts, CancellationToken cancellationToken = default)
    {
        var stdOut = new StringBuilder();
        var command = Cli.Wrap("df").WithArguments([
            path.ToNativeSeparators(_fileSystem.OS),
            "--output=source",
        ]).WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut));

        await ProcessFactory.ExecuteAsync(command, cancellationToken: cancellationToken);
        return ParseFileSystemMount(knownFileSystemMounts, stdOut.ToString());
    }

    internal static FileSystemMount? ParseFileSystemMount(IReadOnlyList<FileSystemMount> knownFileSystemMounts, ReadOnlySpan<char> stdOut)
    {
        var lineEnumerator = stdOut.EnumerateLines();

        // skip header
        if (!lineEnumerator.MoveNext()) return null;
        if (!lineEnumerator.MoveNext()) return null;

        var source = lineEnumerator.Current.ToString();
        return knownFileSystemMounts.FirstOrDefault(x => x.Source.Equals(source, StringComparison.OrdinalIgnoreCase));
    }

    internal static IReadOnlyList<FileSystemMount> ParseFileSystemMounts(IFileSystem fileSystem, ReadOnlySpan<char> stdOut)
    {
        var lineEnumerator = stdOut.EnumerateLines();

        // skip header
        if (!lineEnumerator.MoveNext()) return [];

        var tmp = new List<FileSystemMount>();

        foreach (var line in lineEnumerator)
        {
            if (line.IsWhiteSpace()) continue;

            var splitEnumerator = line.Split(' ');
            var index = 0;

            var source = string.Empty;
            var type = string.Empty;
            var bytesTotal = Size.Zero;
            var bytesAvailable = Size.Zero;
            var target = default(AbsolutePath);

            foreach (var split in splitEnumerator)
            {
                var span = line[split];
                if (span.IsWhiteSpace()) continue;

                if (index == 0)
                {
                    source = span.ToString();
                }
                else if (index == 1)
                {
                    type = span.ToString();
                } else if (index == 2)
                {
                    bytesTotal = BlocksToSize(span);
                } else if (index == 3)
                {
                    bytesAvailable = BlocksToSize(span);
                } else if (index == 4)
                {
                    target = fileSystem.FromUnsanitizedFullPath(span.ToString());
                }

                index += 1;
            }

            tmp.Add(new FileSystemMount(
                Source: source,
                Target: target,
                Type: type,
                BytesTotal: bytesTotal,
                BytesAvailable: bytesAvailable
            ));
        }

        // NOTE(erri120): Need to group by source since one source can have multiple mounting points.
        var results = tmp
            .GroupBy(mount => mount.Source)
            .Select(groups => groups
                .OrderBy(mount => mount.Target.Parts.Count())
                .First()
            )
            .ToArray();

        return results;

        static Size BlocksToSize(ReadOnlySpan<char> input)
        {
            if (!ulong.TryParse(input, out var blocks)) return Size.Zero;
            return Size.From(Size.KB.Value * blocks);
        }
    }
}
