using System.Text;
using CliWrap;
using CliWrap.Exceptions;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Extensions.BCL;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.FileExtractor.Extractors;

/// <summary>
/// Abstracts the 7-zip archive extractor.
/// </summary>
/// <remarks>
///     Uses the 7z binary for each native platform under the hood.
///     We chose this tradeoff due to a lack of decent cross platform option and
///     reportedly possible AVs on invalid archives.
/// </remarks>
public class SevenZipExtractor : IExtractor
{
    private readonly TemporaryFileManager _manager;
    private readonly ILogger<SevenZipExtractor> _logger;

    private static readonly IOSInformation OSInformation = Paths.OSInformation.Shared;

    private static readonly FileType[] SupportedTypesCached = { FileType._7Z, FileType.RAR_NEW, FileType.RAR_OLD, FileType.ZIP };
    private static readonly Extension[] SupportedExtensionsCached = { KnownExtensions._7z, KnownExtensions.Rar, KnownExtensions.Zip, KnownExtensions._7zip };
    private readonly string _exePath;

    /// <inheritdoc />
    public FileType[] SupportedSignatures => SupportedTypesCached;

    /// <inheritdoc />
    public Extension[] SupportedExtensions => SupportedExtensionsCached;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SevenZipExtractor(
        ILogger<SevenZipExtractor> logger,
        TemporaryFileManager fileManager,
        IFileSystem fileSystem)
    {
        _logger = logger;
        _manager = fileManager;

        _exePath = GetExtractorExecutable(fileSystem);
        logger.LogDebug("Using extractor at {ExtractorExecutable}", _exePath);
    }

    /// <inheritdoc />
    public async Task ExtractAllAsync(IStreamFactory sFn, AbsolutePath destination, CancellationToken token)
    {
        await ExtractAllAsync_Impl(sFn, destination, token);
    }

    /// <inheritdoc />
    public async Task<IDictionary<RelativePath, T>> ForEachEntryAsync<T>(IStreamFactory sFn, Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token)
    {
        await using var dest = _manager.CreateFolder();
        await ExtractAllAsync_Impl(sFn, dest, token);

        var results = await dest.Path.EnumerateFiles()
            .SelectAsync(async f =>
            {
                // ReSharper disable once AccessToDisposedClosure
                var path = f.RelativeTo(dest.Path);
                var file = new NativeFileStreamFactory(f);
                var mapResult = await func(path, file);
                f.Delete();
                return KeyValuePair.Create(path, mapResult);
            })
            .Where(d => d.Key != default(RelativePath))
            .ToDictionary();

        return results;
    }

    /// <inheritdoc />
    public Priority DeterminePriority(IEnumerable<FileType> signatures)
    {
        // Yes this is O(n*m) but the search space (should) be very small.
        // 'signatures' should usually be only 1 element :)
        if ((from supported in SupportedSignatures
             from sig in signatures
             where supported == sig
             select supported).Any())
        {
            return Priority.Low;
        }

        return Priority.None;
    }

    private async Task ExtractAllAsync_Impl(IStreamFactory sFn, AbsolutePath destination, CancellationToken token)
    {
        TemporaryPath? spoolFile = null;
        var processStdOutput = new StringBuilder();
        var processStdError = new StringBuilder();
        try
        {
            AbsolutePath source;
            if (sFn.Name is AbsolutePath abs)
            {
                source = abs;
            }
            else
            {
                // File doesn't currently exist on-disk so we need to spool it to disk so we can use 7z against it
                spoolFile = _manager.CreateFile(sFn.Name.FileName.Extension);
                await using var s = await sFn.GetStreamAsync();
                await spoolFile.Value.Path.CopyFromAsync(s, token);
                source = spoolFile.Value.Path;
            }

            _logger.LogDebug("Extracting {Source}", source.FileName);
            var process = Cli.Wrap(_exePath);

            var totalSize = source.FileInfo.Size;
            var lastPercent = 0;

            // NOTE: 7z.exe has a bug with long destination path with forwards `/` separators on windows,
            // as a workaround we need to change the separators to backwards '\' on windows.
            // See: https://sourceforge.net/p/sevenzip/discussion/45797/thread/a9a0f02618/
            var fixedDestination = destination.ToNativeSeparators(OSInformation);

            var result = await process.WithArguments(new[]
                    {
                        "x", "-bsp1", "-y", $"-o{fixedDestination}", source.ToString()
                    }, true)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
                {
                    if (string.IsNullOrWhiteSpace(line)) return;
                    processStdOutput.AppendLine($"[7z stdout] {line}");

                    if (line.Length <= 4 || line[3] != '%') return;
                    if (!int.TryParse(line.AsSpan()[..3], out var percentInt)) return;

                    var oldPosition = lastPercent == 0 ? Size.Zero : totalSize / 100 * lastPercent;
                    var newPosition = percentInt == 0 ? Size.Zero : totalSize / 100 * percentInt;
                    var throughput = newPosition - oldPosition;

                    lastPercent = percentInt;
                }))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
                {
                    if (string.IsNullOrWhiteSpace(line)) return;
                    processStdError.AppendLine($"[7z stderr] {line}");
                }))
                .ExecuteAsync();

            if (result.ExitCode != 0)
                throw new Exception("While executing 7zip");
        }
        catch (CommandExecutionException ex)
        {
            _logger.LogError(ex, "While executing 7zip");
            _logger.LogInformation("Output from the extractor, trying to extract file {File}:\n{StdOutput}\n{StdError}",
                sFn.Name, processStdOutput.ToString(), processStdError.ToString());
            throw;
        }
        finally
        {
            if (spoolFile.HasValue)
            {
                _logger.LogDebug("Cleaning up after extraction");
                await spoolFile.Value.DisposeAsync();
            }
        }
    }

    private static string GetExtractorExecutableFileName()
    {
        return OSInformation.MatchPlatform(
            onWindows: static () => "7z.exe",
            onLinux: static () => "7zz",
            onOSX: static () => "7zz"
        );
    }

    private static string GetExtractorExecutable(IFileSystem fileSystem)
    {
        if (UseSystemExtractor)
        {
            // Depending on the user's distro and package of choice, 7z
            // may have different names, so we'll check for the common ones.
            return !OSInformation.IsLinux
                ? GetExtractorExecutableFileName() :
                FindSystem7zOnLinux();
        }

        var fileName = GetExtractorExecutableFileName();
        var directory = OSInformation.MatchPlatform(
            onWindows: static () => "runtimes/win-x64/native/",
            onLinux: static () => "runtimes/linux-x64/native/",
            onOSX: static () => "runtimes/osx-x64/native/"
        );

        return fileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine(directory + fileName).ToString();
    }

    /// <summary>
    /// Useful for Linux packages that already install 7z globally.
    /// </summary>
    /// <remarks>
    /// See https://github.com/Nexus-Mods/NexusMods.App/issues/1306 for details.
    /// </remarks>
    private const bool UseSystemExtractor =
#if NEXUSMODS_APP_USE_SYSTEM_EXTRACTOR
        true;
#else
        false;
#endif
    
    private static string FindSystem7zOnLinux()
    {
        string[] potentialBinaryNames = ["7z", "7zz", "7zzs"];
        var pathDirectories = Environment.GetEnvironmentVariable("PATH")!.Split(':');

        foreach (var pathDir in pathDirectories)
        foreach (var binaryName in potentialBinaryNames)
        {
            if (File.Exists(Path.Combine(pathDir, binaryName)))
                return binaryName;
        }

        throw new Exception("Cannot find system 7z binary in PATH");
    }
}
