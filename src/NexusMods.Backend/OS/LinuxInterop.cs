using System.Diagnostics;
using System.Text;
using CliWrap;
using LinuxDesktopUtils.XDGDesktopPortal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Backend.RuntimeDependency;
using NexusMods.Paths;
using NexusMods.Sdk;

namespace NexusMods.Backend.OS;

internal partial class LinuxInterop : IOSInterop
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IProcessRunner _processRunner;

    private readonly DesktopPortalConnectionManagerWrapper _portalWrapper;
    private readonly XdgSettingsDependency _xdgSettingsDependency;
    private readonly UpdateDesktopDatabaseDependency _updateDesktopDatabaseDependency;

    public LinuxInterop(IServiceProvider serviceProvider)
    {
        Debug.Assert(serviceProvider.GetRequiredService<IOSInformation>().IsLinux, "this interop only supports Linux");

        _logger = serviceProvider.GetRequiredService<ILogger<LinuxInterop>>();
        _processRunner = serviceProvider.GetRequiredService<IProcessRunner>();
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

        _portalWrapper = new DesktopPortalConnectionManagerWrapper();
        _xdgSettingsDependency = serviceProvider.GetRequiredService<XdgSettingsDependency>();
        _updateDesktopDatabaseDependency = serviceProvider.GetRequiredService<UpdateDesktopDatabaseDependency>();
    }

    public AbsolutePath GetRunningExecutablePath(out string rawPath)
    {
        // https://docs.appimage.org/packaging-guide/environment-variables.html#type-2-appimage-runtime
        // APPIMAGE: (Absolute) path to AppImage file (with symlinks resolved)
        var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE", EnvironmentVariableTarget.Process);
        if (appImagePath is not null)
        {
            rawPath = appImagePath;
            return _fileSystem.FromUnsanitizedFullPath(rawPath);
        }

        rawPath = Environment.ProcessPath ?? throw new NotSupportedException("Unable to get process path");
        return _fileSystem.FromUnsanitizedFullPath(rawPath);
    }

    private async ValueTask<OpenUriPortal> GetPortal()
    {
        var connectionManager = await _portalWrapper.GetInstance();
        var portal = await connectionManager.GetOpenUriPortalAsync();
        return portal;
    }

    public void OpenUri(Uri uri)
    {
        Debug.Assert(!uri.IsFile, $"use {nameof(OpenFile)} for opening file `{uri}`");
        OpenUriImpl(uri).FireAndForget(_logger);
    }

    public void OpenFile(AbsolutePath filePath)
    {
        if (!Helper.AssertIsFile(filePath, _logger)) return;
        OpenFileImpl(filePath).FireAndForget(_logger);
    }

    public void OpenDirectory(AbsolutePath directoryPath)
    {
        if (!Helper.AssertIsDirectory(directoryPath, _logger)) return;

        // NOTE(erri120): the XDG Desktop Portal API works off file descriptors
        // we can't open empty directories, we need at least one file

        if (!directoryPath.EnumerateFiles().TryGetFirst(out var file))
        {
            _logger.LogWarning("Opening empty directories is not supported on Linux");
            return;
        }

        OpenFileInDirectoryImpl(file).FireAndForget(_logger);
    }

    public void OpenFileInDirectory(AbsolutePath filePath)
    {
        OpenFileInDirectoryImpl(filePath).FireAndForget(_logger);
    }

    private async Task OpenUriImpl(Uri uri)
    {
        var portal = await GetPortal();
        await portal.OpenUriAsync(uri);
    }
 
    private async Task OpenFileImpl(AbsolutePath filePath)
    {
        var portal = await GetPortal();
        await portal.OpenFileAsync(file: FilePath.From(filePath.ToNativeSeparators(_fileSystem.OS)));
    }

    private async Task OpenFileInDirectoryImpl(AbsolutePath filePath)
    {
        var portal = await GetPortal();
        await portal.OpenFileInDirectoryAsync(file: FilePath.From(filePath.ToNativeSeparators(_fileSystem.OS)));
    }

    public async ValueTask<FileSystemMount[]> GetFileSystemMounts(CancellationToken cancellationToken = default)
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

        await _processRunner.RunAsync(command, logOutput: true, cancellationToken: cancellationToken);
        return ParseFileSystemMounts(_fileSystem, stdOut.ToString());
    }

    public async ValueTask<FileSystemMount?> GetFileSystemMount(AbsolutePath path, CancellationToken cancellationToken = default)
    {
        var stdOut = new StringBuilder();
        var command = Cli.Wrap("df").WithArguments([
            path.ToNativeSeparators(_fileSystem.OS),
            "--output=source",
        ]).WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut));

        await _processRunner.RunAsync(command, logOutput: true, cancellationToken: cancellationToken);

        var knownFileSystemMounts = await GetFileSystemMounts(cancellationToken: cancellationToken);
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

    internal static FileSystemMount[] ParseFileSystemMounts(IFileSystem fileSystem, ReadOnlySpan<char> stdOut)
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

internal sealed class DesktopPortalConnectionManagerWrapper : IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphoreSlim = new(initialCount: 1, maxCount: 1);
    private DesktopPortalConnectionManager? _instance;

    private async ValueTask<DesktopPortalConnectionManager> InitAsync()
    {
        await _semaphoreSlim.WaitAsync(timeout: TimeSpan.FromSeconds(10));

        var manager = await DesktopPortalConnectionManager.ConnectAsync();
        _instance = manager;

        return manager;
    }

    public ValueTask<DesktopPortalConnectionManager> GetInstance()
    {
        if (_instance is not null) return new ValueTask<DesktopPortalConnectionManager>(_instance);
        return InitAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_instance is not null) await _instance.DisposeAsync();
    }
}

