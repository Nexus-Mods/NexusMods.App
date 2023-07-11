using System.Runtime.CompilerServices;
using System.Text;
using FomodInstaller.Scripting.XmlScript;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.JsonConverters;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.FOMOD;

public class FomodAnalyzer : IFileAnalyzer
{
    public FileAnalyzerId Id { get; } = FileAnalyzerId.New("e5dcce84-ad7c-4882-8873-4f6a2e45a279", 1);

    private readonly ILogger<FomodAnalyzer> _logger;
    private readonly IFileSystem _fileSystem;

    // Note: No type for .fomod because FOMODs are existing archive types listed below.
    public FomodAnalyzer(ILogger<FomodAnalyzer> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public IEnumerable<FileType> FileTypes { get; } = new [] { FileType.XML };

    public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Not sourced from an archive.
        if (info.RelativePath == null)
            yield break;

        // Check if file path is "fomod/ModuleConfig.xml"
        if (!info.RelativePath.Value.EndsWith(FomodConstants.XmlConfigRelativePath))
            yield break;

        // If not from inside an archive, this is probably not a FOMOD.
        if (info.ParentArchive == null)
            yield break;
        
        // If the fomod folder is not at first level, find the prefix.
        var pathPrefix = info.RelativePath.Value.Parent.Parent;

        // Now get the actual items out.
        // Determine if this is a supported FOMOD.
        string? data;
        var images = new List<FomodAnalyzerInfo.FomodAnalyzerImage>();

        try
        {
            using var streamReader = new StreamReader(info.Stream, leaveOpen:true);
            data = await streamReader.ReadToEndAsync(ct);
            var xmlScript = new XmlScriptType();
            var script = (XmlScript)xmlScript.LoadScript(data, true);

            // Get all images listed in the FOMOD script.
            async Task AddImageIfValid(string? imagePathFragment)
            {
                if (string.IsNullOrEmpty(imagePathFragment))
                    return;
                var imagePath = pathPrefix.Join(PathHelpers.Sanitize(imagePathFragment, _fileSystem.OS));
                
                var path = info.ParentArchive!.Value.Path.Combine(imagePath);
                byte[] bytes;
                try
                {
                    bytes = await path.ReadAllBytesAsync(ct);
                }
                catch (FileNotFoundException)
                {
                    bytes = await GetPlaceholderImage(ct);
                }

                images!.Add(new FomodAnalyzerInfo.FomodAnalyzerImage(imagePath, bytes));
            }

            await AddImageIfValid(script.HeaderInfo.ImagePath);
            foreach (var step in script.InstallSteps)
            foreach (var optionGroup in step.OptionGroups)
            foreach (var option in optionGroup.Options)
                await AddImageIfValid(option.ImagePath);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to Parse FOMOD: {EMessage}\\n{EStackTrace}", e.Message, e.StackTrace);
            yield break;
        }

        // TODO: We use Base64 here, which is really, really inefficient. We should zip up the images and store them separately in a non-SOLID archive.
        // Add all images to analysis output.
        yield return new FomodAnalyzerInfo()
        {
            XmlScript = data!,
            Images = images
        };
    }

    internal async Task<byte[]> GetPlaceholderImage(CancellationToken ct = default)
    {
        return await _fileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Assets/InvalidImagePlaceholder.png")
            .ReadAllBytesAsync(ct);
    }
}

[JsonName("NexusMods.Games.FOMOD.FomodAnalyzerInfo")]
public record FomodAnalyzerInfo : IFileAnalysisData
{
    public required string XmlScript { get; init; }
    public required List<FomodAnalyzerImage> Images { get; init; }

    public record struct FomodAnalyzerImage(string Path, byte[] Image);

    // Keeping in case this is ever needed. We can remove this once all FOMOD stuff is done.
    [PublicAPI]
    public async Task DumpToFileSystemAsync(TemporaryPath fomodFolder)
    {
        var fs = FileSystem.Shared;
        var path = fomodFolder.Path;

        // Dump Item
        async Task DumpItem(string relativePath, byte[] data)
        {
            var finalPath = path.Combine(relativePath);
            fs.CreateDirectory(finalPath.Parent);
            await fs.WriteAllBytesAsync(finalPath, data);
        }

        // Dump Xml
        await DumpItem(FomodConstants.XmlConfigRelativePath, Encoding.UTF8.GetBytes(XmlScript));

        // Dump Images
        foreach (var image in Images)
            await DumpItem(image.Path, image.Image);
    }
}
