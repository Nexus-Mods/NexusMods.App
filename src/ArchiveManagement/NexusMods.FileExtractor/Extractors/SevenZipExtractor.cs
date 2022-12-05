using System.Runtime.InteropServices;
using CliWrap;
using CliWrap.Exceptions;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Interfaces;
using NexusMods.Interfaces.Streams;
using NexusMods.Paths;
using Wabbajack.Common.FileSignatures;

namespace NexusMods.FileExtractor.Extractors;

public class SevenZipExtractor : IExtractor
{
    private readonly TemporaryFileManager _manager;
    private readonly ILogger<SevenZipExtractor> _logger;
    private readonly IResource<IExtractor,Size> _limiter;

    public SevenZipExtractor(ILogger<SevenZipExtractor> logger, TemporaryFileManager fileManager, IResource<IExtractor, Size> limiter)
    {
        _logger = logger;
        _manager = fileManager;
        _limiter = limiter;
    }
    
    public async Task<IDictionary<RelativePath, T>> ForEachEntry<T>(IStreamFactory sFn, Func<RelativePath, IStreamFactory, ValueTask<T>> func, CancellationToken token)
    {
        TemporaryPath? tmpFile = null;
        await using var dest = _manager.CreateFolder();

        TemporaryPath? spoolFile = null;
        AbsolutePath source;
        
        var job = await _limiter.Begin($"Extracting {sFn.Name}", sFn.Size, token);
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

            process = process.WithArguments(
                new[]
                {
                    "x", "-bsp1", "-y", $"-o{dest}", source.ToString(), "-mmt=off"
                }, true);

            var totalSize = source.Length;
            var lastPercent = 0;
            job.Size = totalSize;

            var result = await process.ExecuteAsync();

            if (result.ExitCode != 0)
                throw new Exception("While executing 7zip");

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

    private static string ExeLocation()
    {
        var initialPath = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            initialPath = @"Extractors\windows-x64\7z.exe";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            initialPath = @"Extractors\linux-x64\7zz";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            initialPath = @"Extractors\mac\7zz";
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

    public IEnumerable<FileType> SupportedSignatures => new[] { FileType._7Z, FileType.RAR_NEW, FileType.RAR_OLD, FileType.ZIP };
    public IEnumerable<Extension> SupportedExtensions => new[] { Ext._7z, Ext.Rar, Ext.Zip, Ext._7zip };
}