using System.Runtime.InteropServices;
using CliWrap;
using CliWrap.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexusMods.Common;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Interfaces;
using NexusMods.Paths;

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
    private readonly IResource<IExtractor,Size> _limiter;

    private static readonly FileType[] SupportedTypesCached = { FileType._7Z, FileType.RAR_NEW, FileType.RAR_OLD, FileType.ZIP };
    private static readonly Extension[] SupportedExtensionsCached = { Ext._7z, Ext.Rar, Ext.Zip, Ext._7zip };
    private static readonly string ExePath = GetExeLocation().ToRelativePath().RelativeTo(KnownFolders.EntryFolder).ToString();

    /// <inheritdoc />
    public FileType[] SupportedSignatures => SupportedTypesCached;

    /// <inheritdoc />
    public Extension[] SupportedExtensions => SupportedExtensionsCached;

    /// <summary>
    /// Creates a 7-zip based extractor.
    /// </summary>
    /// <param name="logger">Provides logger support. Use <see cref="NullLogger.Instance"/> if you don't want logging.</param>
    /// <param name="fileManager">Manager that can be used to create temporary folders.</param>
    /// <param name="limiter">Limits CPU core usage depending on our use case.</param>
    public SevenZipExtractor(ILogger<SevenZipExtractor> logger, TemporaryFileManager fileManager, IResource<IExtractor, Size> limiter)
    {
        _logger = logger;
        _manager = fileManager;
        _limiter = limiter;
    }

    /// <inheritdoc />
    public async Task ExtractAllAsync(IStreamFactory sFn, AbsolutePath destination, CancellationToken token)
    {
        using var job = await _limiter.Begin($"[${nameof(ExtractAllAsync)}] Extracting {sFn.Name.FileName}", sFn.Size, token);
        await ExtractAllAsync_Impl(sFn, destination, token, job);
    }

    /// <inheritdoc />
    public async Task<IDictionary<RelativePath, T>> ForEachEntryAsync<T>(IStreamFactory sFn, Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token)
    {
        using var job = await _limiter.Begin($"[${nameof(ForEachEntryAsync)}] Extracting {sFn.Name.FileName}", sFn.Size, token);
        await using var dest = _manager.CreateFolder();
        await ExtractAllAsync_Impl(sFn, dest, token, job);
        
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
            .Where(d => d.Key != default)
            .ToDictionary();
        
        return results;
    }

    /// <inheritdoc />
    public Priority DeterminePriority(IEnumerable<FileType> signatures)
    {
        // Yes this is O(n*m) but the search space (should) be very small. 
        if ((from supported in SupportedSignatures 
                from sig in signatures 
                where supported == sig 
                select supported).Any())
        {
            return Priority.Low;
        }

        return Priority.None;
    }
    
    private async Task ExtractAllAsync_Impl(IStreamFactory sFn, AbsolutePath destination, CancellationToken token, IJob<IExtractor, Size> job)
    {
        TemporaryPath? spoolFile = null;
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
                await using var s = await sFn.GetStream();
                await spoolFile.Value.Path.CopyFromAsync(s, token);
                source = spoolFile.Value.Path;
            }

            _logger.LogDebug("Extracting {Source}", source.FileName);
            var process = Cli.Wrap(ExePath);

            var totalSize = source.Length;
            var lastPercent = 0;
            job.Size = totalSize;

            var result = await process.WithArguments(new[]
                    {
                        "x", "-bsp1", "-y", $"-o{destination}", source.ToString(), "-mmt=off"
                    }, true)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
                {
                    if (line.Length <= 4 || line[3] != '%') return;
                    if (!int.TryParse(line.AsSpan()[..3], out var percentInt)) return;

                    var oldPosition = lastPercent == 0 ? Size.Zero : totalSize / 100 * lastPercent;
                    var newPosition = percentInt == 0 ? Size.Zero : totalSize / 100 * percentInt;
                    var throughput = newPosition - oldPosition;
                    if (throughput > Size.Zero)
                        job.ReportNoWait(throughput);

                    lastPercent = percentInt;
                }))
                .ExecuteAsync();

            if (result.ExitCode != 0)
                throw new Exception("While executing 7zip");
        }
        catch (CommandExecutionException ex)
        {
            _logger.LogError(ex, "While executing 7zip");
            throw;
        }
        finally
        {
            _logger.LogDebug("Cleaning up after extraction");
            await spoolFile.DisposeIfNotNullAsync();
        }
    }
    
    private static string GetExeLocation()
    {
        var initialPath = "";
        if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new NotSupportedException($"{nameof(NexusMods.FileExtractor)}'s {nameof(SevenZipExtractor)} only supports x64 processors.");
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            initialPath = @"runtimes\win-x64\native\7z.exe";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            initialPath = @"runtimes\linux-x64\native\7zz";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            initialPath = @"runtimes\osx-x64\native\7zz";
        return initialPath;
    }
}