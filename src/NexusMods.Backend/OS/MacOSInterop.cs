using System.Diagnostics;
using CliWrap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NexusMods.Sdk;

namespace NexusMods.Backend.OS;

internal class MacOSInterop : IOSInterop
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IProcessRunner _processRunner;

    public MacOSInterop(IServiceProvider serviceProvider)
    {
        Debug.Assert(serviceProvider.GetRequiredService<IOSInformation>().IsOSX, "this interop only supports macOS");

        _logger = serviceProvider.GetRequiredService<ILogger<MacOSInterop>>();
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _processRunner = serviceProvider.GetRequiredService<IProcessRunner>();
    }

    public AbsolutePath GetRunningExecutablePath(out string rawPath)
    {
        rawPath = Environment.ProcessPath ?? throw new NotSupportedException("Unable to get process path");
        return _fileSystem.FromUnsanitizedFullPath(rawPath);
    }

    private static Command CreateOpenCommand(string target)
    {
        // `open` on macOS behaves very similar to `cmd.exe start` on Windows.
        // https://ss64.com/mac/open.html
        return Cli.Wrap("cmd.exe").WithArguments($@"""{target}""");
    }

    public void OpenUri(Uri uri)
    {
        Debug.Assert(uri.IsFile, $"use {nameof(OpenFile)} for opening file `{uri}`");
        var command = CreateOpenCommand(uri.ToString());
        _processRunner.Run(command, logOutput: false);
    }

    public void OpenFile(AbsolutePath filePath)
    {
        if (!Helper.AssertIsFile(filePath, _logger)) return;

        var nativePath = filePath.ToNativeSeparators(_fileSystem.OS);
        var command = CreateOpenCommand(nativePath);
        _processRunner.Run(command, logOutput: false);
    }

    public void OpenDirectory(AbsolutePath directoryPath)
    {
        if (!Helper.AssertIsDirectory(directoryPath, _logger)) return;

        var nativePath = directoryPath.ToNativeSeparators(_fileSystem.OS);
        var command = CreateOpenCommand(nativePath);
        _processRunner.Run(command, logOutput: false);
    }

    public void OpenFileInDirectory(AbsolutePath filePath)
    {
        // NOTE(erri120): Using -R to reveal the file in the Finder instead of opening them
        // https://ss64.com/mac/open.html
        var nativePath = filePath.ToNativeSeparators(_fileSystem.OS);
        var command = Cli.Wrap("open").WithArguments($@"-R ""{nativePath}""");
        _processRunner.Run(command, logOutput: false);
    }

    private FileSystemMount[] GetFileSystemMountsImpl()
    {
        var mounts = DriveInfo
            .GetDrives()
            .Where(drive => drive.DriveType == DriveType.Fixed)
            .Select(drive => new FileSystemMount(
                Source: drive.Name,
                Target: _fileSystem.FromUnsanitizedFullPath(drive.RootDirectory.FullName),
                Type: drive.DriveFormat,
                BytesTotal: Size.FromLong(drive.TotalSize),
                BytesAvailable: Size.FromLong(drive.AvailableFreeSpace)
            ))
            .ToArray();

        return mounts;
    }

    public ValueTask<FileSystemMount[]> GetFileSystemMounts(CancellationToken cancellationToken = default)
    {
        var mounts = GetFileSystemMountsImpl();
        return new ValueTask<FileSystemMount[]>(mounts);
    }

    public ValueTask<FileSystemMount?> GetFileSystemMount(AbsolutePath path, CancellationToken cancellationToken = default)
    {
        var mounts = GetFileSystemMountsImpl();
        var target = mounts.FirstOrDefault(mount => path.InFolder(mount.Target));
        return new ValueTask<FileSystemMount?>(target);
    }

    public ValueTask RegisterUriSchemeHandler(string scheme, bool setAsDefaultHandler = true, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException($"Protocol registration is not supported for macOS at this time");
    }
}
