using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.BethesdaGameStudios.Installers;

public class LooseFileInstaller : IModInstaller
{
    private (RelativePath Prefix, FileType Type)[] _prefixes;
    private readonly IDataStore _store;

    public LooseFileInstaller(IDataStore store)
    {
        _store = store;
        _prefixes = new[]
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
            return Common.Priority.None;

        return FilterFiles(files).Any()
            ? Common.Priority.Normal
            : Common.Priority.None;
    }

    private IEnumerable<(RelativePath Path, AnalyzedFile Entry)> FilterFiles(EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        return from kv in files
               from prefix in _prefixes
               where kv.Key.InFolder(prefix.Prefix)
               select (kv.Key, kv.Value);
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        return FilterFiles(files)
            .Select(file =>
            {
                var outFile = file.Path;
                if (!file.Path.InFolder(_dataFolder))
                    outFile = _dataFolder.Join(file.Path);

                return new FromArchive
                {
                    Id = ModFileId.New(),
                    From = new HashRelativePath(srcArchive, file.Path),
                    To = new GamePath(GameFolderType.Game, outFile),
                    Hash = file.Entry.Hash,
                    Size = file.Entry.Size
                };
            });
    }
}
