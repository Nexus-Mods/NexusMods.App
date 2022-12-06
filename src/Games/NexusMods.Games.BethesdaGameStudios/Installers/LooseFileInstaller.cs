using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.ModLists;
using NexusMods.DataModel.ModLists.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Interfaces;
using NexusMods.Paths;
using Wabbajack.Common.FileSignatures;

namespace NexusMods.Games.BethesdaGameStudios.Installers;

public class LooseFileInstaller : IModInstaller
{
    private (RelativePath Prefix, FileType Type)[] _prefixes;
    private readonly IDataStore _store;

    public LooseFileInstaller(IDataStore store)
    {
        _store = store;
        _prefixes = new []
        {
            ("meshes".ToRelativePath(), FileType.NIF),
            ("textures".ToRelativePath(), FileType.DDS)
        };
        _prefixes = _prefixes.Concat(_prefixes.Select(p => (_dataFolder.Join(p.Prefix), p.Type)))
            .ToArray();

    }

    private RelativePath _dataFolder = "Data".ToRelativePath();


    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (installation.Game is not SkyrimSpecialEdition)
            return Interfaces.Priority.None;

        return FilterFiles(files).Any()
            ? Interfaces.Priority.Normal
            : Interfaces.Priority.None;
    }

    private IEnumerable<RelativePath> FilterFiles(EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        return from file in files.Keys
            from prefix in _prefixes
            where file.InFolder(prefix.Prefix)
            select file;
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        return FilterFiles(files)
            .Select(file =>
            {
                var outFile = file;
                if (!file.InFolder(_dataFolder))
                    outFile = _dataFolder.Join(file);
                
                return new FromArchive
                {
                    From = new HashRelativePath(srcArchive, file),
                    To = new GamePath(GameFolderType.Game, outFile),
                    Store = _store
                };
            });
    }
}