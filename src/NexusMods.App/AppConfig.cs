using NexusMods.Abstractions.FileExtractor;
using NexusMods.DataModel;
using NexusMods.Paths;

namespace NexusMods.App;

/// <summary>
/// The configuration for the Nexus Mods App.
/// </summary>
public class AppConfig
{
    public AppConfig()
    {
        var fileSystem = FileSystem.Shared;
        DataModelSettings = new DataModelSettings(fileSystem);
        FileExtractorSettings = new FileExtractorSettings(fileSystem);
    }

    public DataModelSettings DataModelSettings { get; set; }
    public FileExtractorSettings FileExtractorSettings { get; set; }
    public bool? EnableTelemetry { get; set; }

    /// <summary>
    /// Sanitizes the config; e.g.
    /// </summary>
    public void Sanitize(IFileSystem fs)
    {
        DataModelSettings.Sanitize(fs);
        FileExtractorSettings.Sanitize(fs);
    }
}

