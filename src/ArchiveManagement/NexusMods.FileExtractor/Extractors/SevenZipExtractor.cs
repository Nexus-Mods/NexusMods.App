using System.Runtime.InteropServices;
using System.Text;
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

    /// <inheritdoc />
    public IEnumerable<FileType> SupportedSignatures => new[] { FileType._7Z, FileType.RAR_NEW, FileType.RAR_OLD, FileType.ZIP };
    
    /// <inheritdoc />
    public IEnumerable<Extension> SupportedExtensions => new[] { Ext._7z, Ext.Rar, Ext.Zip, Ext._7zip };
    
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
    public async Task<IDictionary<RelativePath, T>> ForEachEntryAsync<T>(IStreamFactory sFn, Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token)
    {
        TemporaryPath? tmpFile = null;
        await using var dest = _manager.CreateFolder();

        TemporaryPath? spoolFile = null;
        AbsolutePath source;
        
        var job = await _limiter.Begin($"Extracting {sFn.Name.FileName}", sFn.Size, token);
        try
        {
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

            var initialPath = ExeLocation();

            var process = Cli.Wrap(initialPath.ToRelativePath().RelativeTo(KnownFolders.EntryFolder).ToString());
            
            var totalSize = source.Length;
            var lastPercent = 0;
            job.Size = totalSize;

            var errors = new StringBuilder();
            try
            {

                var result = await process.WithArguments(
                        new[]
                        {
                            "x", "-bsp1", "-y", $"-o{dest}", source.ToString(), "-mmt=off"
                        }, true)
                    .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
                    {
                        if (line.Length <= 4 || line[3] != '%') return;

                        if (!int.TryParse(line[..3], out var percentInt)) return;

                        var oldPosition = lastPercent == 0 ? Size.Zero : totalSize / 100 * lastPercent;
                        var newPosition = percentInt == 0 ? Size.Zero : totalSize / 100 * percentInt;
                        var throughput = newPosition - oldPosition;
                        job.ReportNoWait(throughput);

                        lastPercent = percentInt;
                    }))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(errors))
                    .ExecuteAsync();
                
                if (result.ExitCode != 0)
                    throw new Exception("While executing 7zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "While executing 7zip: {Errors}", errors);
                throw;
            }
            
            job.Dispose();
            var results = await dest.Path.EnumerateFiles()
                .SelectAsync(async f =>
                {
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
        catch (CommandExecutionException ex)
        {
            _logger.LogError(ex, "While executing 7zip");
            throw;
        }
        finally
        {
            _logger.LogDebug("Cleaning up after extraction");
            
            if (tmpFile != null) await tmpFile.Value.DisposeAsync();
            if (spoolFile != null) await spoolFile.Value.DisposeAsync();
            job.Dispose();
        } 
    }

    /// <inheritdoc />
    public async Task ExtractAllAsync(IStreamFactory sFn, AbsolutePath destination, CancellationToken token)
    {
        TemporaryPath? tmpFile = null;
        
        TemporaryPath? spoolFile = null;
        AbsolutePath source;
        
        var job = await _limiter.Begin($"Extracting {sFn.Name.FileName}", sFn.Size, token);
        try
        {
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

            var initialPath = ExeLocation();

            var process = Cli.Wrap(initialPath.ToRelativePath().RelativeTo(KnownFolders.EntryFolder).ToString());
            
            var totalSize = source.Length;
            var lastPercent = 0;
            job.Size = totalSize;

            var result = await process.WithArguments(
                new[]
                {
                    "x", "-bsp1", "-y", $"-o{destination}", source.ToString(), "-mmt=off"
                }, true)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
                {
                    if (line.Length <= 4 || line[3] != '%') return;

                    if (!int.TryParse(line[..3], out var percentInt)) return;

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

            job.Dispose();
        }
        catch (CommandExecutionException ex)
        {
            _logger.LogError(ex, "While executing 7zip");
            throw;
        }
        finally
        {
            _logger.LogDebug("Cleaning up after extraction");
            
            if (tmpFile != null) await tmpFile.Value.DisposeAsync();
            if (spoolFile != null) await spoolFile.Value.DisposeAsync();
            job.Dispose();
        } 
    }

    private static string ExeLocation()
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
}