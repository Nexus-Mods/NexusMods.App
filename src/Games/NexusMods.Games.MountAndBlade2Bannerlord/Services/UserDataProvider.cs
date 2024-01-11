using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Options;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Services;

internal sealed class UserDataProvider
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    public UserDataProvider(ILogger<UserDataProvider> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    private AbsolutePath GetLauncherDataPath()
    {
        var documentsFolder = _fileSystem.GetKnownPath(KnownPath.MyDocumentsDirectory);
        return documentsFolder.Combine($"{MountAndBlade2BannerlordConstants.DocumentsFolderName}/Configs/LauncherData.xml");
    }

    // We could potentially import some settings from here
    public UserData? LoadUserData()
    {
        var path = GetLauncherDataPath();
        var xmlSerializer = new XmlSerializer(typeof(UserData));
        try
        {
            using var xmlReader = XmlReader.Create(path.Read());
            return xmlSerializer.Deserialize(xmlReader) as UserData;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to deserialize Bannerlord LauncherData file!");
            return null;
        }
    }

    public void SaveUserData(UserData userData)
    {
        var path = GetLauncherDataPath();
        var xmlSerializer = new XmlSerializer(typeof(UserData));
        try
        {
            using var xmlWriter = XmlWriter.Create(path.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read), new XmlWriterSettings
            {
                Indent = true
            });
            xmlSerializer.Serialize(xmlWriter, userData);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to serialize Bannerlord LauncherData file!");
        }
    }
}
