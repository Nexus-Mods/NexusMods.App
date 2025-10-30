using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reactive.Disposables;
using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.FileExtractor;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
using NexusMods.Sdk;
using NexusMods.Sdk.IO;
using Reloaded.Memory.Extensions;

namespace NexusMods.Backend.FileExtractor.Extractors;

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
    private static readonly FileType[] SupportedTypesCached = [FileType._7Z, FileType.RAR_NEW, FileType.RAR_OLD, FileType.ZIP];
    private static readonly Extension[] SupportedExtensionsCached = [KnownExtensions._7z, KnownExtensions.Rar, KnownExtensions.Zip, KnownExtensions._7zip];

    /// <inheritdoc />
    public FileType[] SupportedSignatures => SupportedTypesCached;

    /// <inheritdoc />
    public Extension[] SupportedExtensions => SupportedExtensionsCached;

    private readonly ILogger _logger;
    private readonly IOSInformation _osInformation;
    private readonly IProcessRunner _processRunner;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly string _exePath;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SevenZipExtractor(
        ILogger<SevenZipExtractor> logger,
        TemporaryFileManager fileTemporaryFileManager,
        IFileSystem fileSystem, 
        IProcessRunner processRunner,
        IOSInformation osInformation)
    {
        _logger = logger;
        _temporaryFileManager = fileTemporaryFileManager;
        _processRunner = processRunner;
        _osInformation = osInformation;
        
        _exePath = GetExtractorExecutable(fileSystem, osInformation);
        logger.LogDebug("Using extractor at {ExtractorExecutable}", _exePath);
    }

    /// <inheritdoc />
    public async Task ExtractAllAsync(IStreamFactory streamFactory, AbsolutePath destination, CancellationToken cancellationToken)
    {
        var (source, sourceDisposable) = await GetSource(streamFactory, cancellationToken: cancellationToken);
        using var _ = sourceDisposable;

        var trimmablePathsInArchive = await GetTrimmablePathsInArchive(source, cancellationToken: cancellationToken);
        await ExtractArchive(source, destination, cancellationToken: cancellationToken);

        if (trimmablePathsInArchive.Count == 0) return;
        FixPaths(trimmablePathsInArchive, destination);
    }

    [SuppressMessage("ReSharper", "RedundantNameQualifier")]
    private void FixPaths(
        IReadOnlyList<(string fileName, bool isDirectory)> trimmablePathsInArchive,
        AbsolutePath destinationPath)
    {
        // NOTE(erri120): We need to fix paths that end in whitespace as they cause a lot of issues.
        // On Linux, 7z extracts "foo/bar " to "foo/bar " properly, but our path sanitization will trim the path and the app won't find the file on disk
        // On Windows, 7z changes "foo/bar " to "far/bar_" because paths ending with whitespace aren't supported on Windows
        // See https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file

        var isWindows = _osInformation.IsWindows;
        var nativePath = destinationPath.ToNativeSeparators(_osInformation);

        const int maxBufferSize = 512;
        Span<char> span = stackalloc char[maxBufferSize];

        foreach (var tuple in trimmablePathsInArchive)
        {
            var (fileName, isDirectory) = tuple;

            string fileNameOnDisk;
            if (isWindows)
            {
                if (fileName.Length > maxBufferSize) throw new NotSupportedException($"File name is too long: `{fileName}`");
                fileName.AsSpan().CopyTo(span);

                var slice = span.SliceFast(start: 0, length: fileName.Length);
                To7ZipWindowsExtractionPath(slice);

                fileNameOnDisk = slice.ToString();
            }
            else
            {
                fileNameOnDisk = fileName;
            }

            var fixedFileName = PathsHelper.FixFileName(fileName);
            Debug.Assert(fixedFileName.All(c => !PathsHelper.IsInvalidChar(c)), message: $"`{fixedFileName}` should be fixed");

            var source = System.IO.Path.Combine(nativePath, fileNameOnDisk);
            var destination = System.IO.Path.Combine(nativePath, fixedFileName);

            if (isDirectory)
            {
                try
                {
                    _logger.LogWarning("Fixing path by moving directory from `{From}` to `{To}`", source, destination);
                    if (System.IO.Directory.Exists(destination))
                    {
                        _logger.LogWarning("Destination directory `{Destination}` already exists, deleting source instead", destination);
                        System.IO.Directory.Delete(source, recursive: true);
                    }
                    else
                    {
                        System.IO.Directory.Move(source, destination);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to fix path by moving directory from `{From}` to `{To}`", source, destination);
                }
            }
            else
            {
                try
                {
                    _logger.LogWarning("Fixing path by moving file from `{From}` to `{To}`", source, destination);
                    if (System.IO.File.Exists(destination))
                    {
                        _logger.LogWarning("Destination file `{Destination}` already exists, deleting source instead", destination);
                        System.IO.File.Delete(source);
                    }
                    else
                    {
                        System.IO.File.Move(source, destination);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to fix path by moving file from `{From}` to `{To}`", source, destination);
                }
            }
        }
    }

    internal static void To7ZipWindowsExtractionPath(Span<char> input)
    {
        // https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file
        // https://github.com/mcmilk/7-Zip/blob/c44df79f9a65142c460313f720dc22c8783c63b1/CPP/7zip/UI/Common/ExtractingFilePath.cpp#L53-L88
        // 7zip creates "safe" Windows paths, so we'll mirror the Microsoft recommendations:

        // Do not end a file or directory name with a space or a period.
        // Although the underlying file system may support such names, the Windows shell and user interface does not.
        // However, it is acceptable to specify a period as the first character of a name. For example, ".temp".

        for (var i = input.Length - 1; i >= 0; i--)
        {
            var current = input[i];
            if (!PathsHelper.IsInvalidChar(current)) break;

            input[i] = '_';
        }
    }

    /// <summary>
    /// Returns a list of all paths in the archive that need to be trimmed.
    /// </summary>
    private async ValueTask<IReadOnlyList<(string fileName, bool isDirectory)>> GetTrimmablePathsInArchive(AbsolutePath source, CancellationToken cancellationToken)
    {
        var pathsWithInvalidCharacters = new List<(string fileName, bool isDirectory)>();

        source = source.FileSystem.Map(source);
        // NOTE(erri120): "l -ba" is an undocumented command that skips the table header and footer
        var process = Cli
            .Wrap(_exePath)
            .WithArguments(["l", "-ba", $"{source.ToNativeSeparators(_osInformation)}"])
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
            {
                if (!TryParseListCommandOutput(line, out var fileName, out var isDirectory)) return;
                pathsWithInvalidCharacters.Add((fileName, isDirectory));
            }))
            .WithValidation(CommandResultValidation.ZeroExitCode);

        await _processRunner.RunAsync(process, logOutput: true, cancellationToken: cancellationToken);
        return pathsWithInvalidCharacters;
    }

    /// <summary>
    /// Parses a table row returned by the 7z list command.
    /// </summary>
    internal static bool TryParseListCommandOutput(ReadOnlySpan<char> line, [NotNullWhen(true)] out string? fileName, out bool isDirectory)
    {
        // The table that gets printed has a fixed minimum width:
        // https://github.com/btolab/p7zip/blob/f30c859433af90937723dd4e808a24c3bb836711/CPP/7zip/UI/Console/List.cpp#L186-L193
        const int fixedLengthBeforeAttributes = 20;
        const int fixedLengthBeforeFileName = 53;

        fileName = null;
        isDirectory = false;

        if (line.Length < fixedLengthBeforeFileName) return false;

        var attributesSlice = line.SliceFast(start: fixedLengthBeforeAttributes);
        isDirectory = attributesSlice[0] == 'D';

        var fileNameSlice = line.SliceFast(start: fixedLengthBeforeFileName);

        // NOTE(erri120): "." and ".." can for some reason end up in the archive
        // 7z doesn't extract them so we should just ignore them
        if (fileNameSlice.Length == 1 && fileNameSlice[0] == '.') return false;
        if (fileNameSlice.Length == 2 && fileNameSlice[0] == '.' && fileNameSlice[1] == '.') return false;

        var lastChar = fileNameSlice[^1];
        if (!PathsHelper.IsInvalidChar(lastChar)) return false;

        fileName = fileNameSlice.ToString();
        return true;
    }

    private async ValueTask<(AbsolutePath, IDisposable)> GetSource(IStreamFactory streamFactory, CancellationToken cancellationToken)
    {
        if (streamFactory is NativeFileStreamFactory nativeFileStreamFactory) return (nativeFileStreamFactory.Path, Disposable.Empty);

        var temporaryFile = _temporaryFileManager.CreateFile(ext: streamFactory.FileName.Extension);
        await using var stream = await streamFactory.GetStreamAsync();
        await temporaryFile.Path.CopyFromAsync(stream, cancellationToken);

        return (temporaryFile.Path, temporaryFile);
    }

    private async ValueTask ExtractArchive(AbsolutePath source, AbsolutePath destination, CancellationToken cancellationToken)
    {
        destination = destination.FileSystem.Map(destination);
        source = source.FileSystem.Map(source);
        // NOTE(erri120): Using "-bsp1" to redirect the progress line to stdout
        var process = Cli
            .Wrap(_exePath)
            .WithArguments(["x", "-bsp1", "-y", $"-o{destination.ToNativeSeparators(_osInformation)}", $"{source.ToNativeSeparators(_osInformation)}"])
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
            {
                // TODO: progress reporting
                _ = TryParseExtractCommandOutput(line, out var percentage);
            }))
            .WithValidation(CommandResultValidation.ZeroExitCode);

        await _processRunner.RunAsync(process, logOutput: true, cancellationToken: cancellationToken);
    }

    private static bool TryParseExtractCommandOutput(ReadOnlySpan<char> line, out int percentage)
    {
        // "  0%"
        // " 10%"
        // "100%"
        const int percentageIndex = 3;
        percentage = 0;

        if (line.Length < percentageIndex + 1) return false;
        if (line[percentageIndex] != '%') return false;

        var rawPercentage = line.SliceFast(start: 0, length: percentageIndex);
        return int.TryParse(rawPercentage, style: NumberStyles.None, provider: NumberFormatInfo.InvariantInfo, out percentage);
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

    private static string GetExtractorExecutableFileName(IOSInformation osInformation)
    {
        return osInformation.MatchPlatform(
            onWindows: static () => "7z.exe",
            onLinux: static () => "7zz",
            onOSX: static () => "7zz"
        );
    }

    private static string GetExtractorExecutable(IFileSystem fileSystem, IOSInformation osInformation)
    {
#pragma warning disable CS0162 // Unreachable code detected
        if (UseSystemExtractor)
        {
            // Depending on the user's distro and package of choice, 7z
            // may have different names, so we'll check for the common ones.
            return !osInformation.IsLinux
                ? GetExtractorExecutableFileName(osInformation) :
                FindSystem7zOnLinux();
        }

        var fileName = GetExtractorExecutableFileName(osInformation);
        var directory = osInformation.MatchPlatform(
            onWindows: static () => "runtimes/win-x64/native/",
            onLinux: static () => "runtimes/linux-x64/native/",
            onOSX: static () => "runtimes/osx-x64/native/"
        );

        return fileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine(directory + fileName).ToString();
#pragma warning restore CS0162 // Unreachable code detected
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
