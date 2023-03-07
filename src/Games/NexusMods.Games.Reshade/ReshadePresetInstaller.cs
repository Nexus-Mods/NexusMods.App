using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Games.Abstractions;
using NexusMods.Games.Generic.Entities;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.Reshade;

public class ReshadePresetInstaller : IModInstaller
{
    private static HashSet<RelativePath> _ignoreFiles = new[]
    {
        "readme.txt",
        "installation.txt",
        "license.txt"
    }.Select(t => t.ToRelativePath())
     .ToHashSet();

    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var filtered = files.Where(f => !_ignoreFiles.Contains(f.Key.FileName))
            .ToList();

        // We have to be able to find the game's executable
        if (installation.Game is not AGame)
            return Common.Priority.None;

        // We only support ini files for now
        if (!filtered.All(f => f.Value.FileTypes.Contains(FileType.INI)))
            return Common.Priority.None;

        // Get all the ini data
        var iniData = filtered
            .Select(f => f.Value.AnalysisData
                .OfType<IniAnalysisData>()
                .FirstOrDefault())
            .Where(d => d is not null)
            .Select(d => d!)
            .ToList();

        // All the files must have ini data
        if (iniData.Count != filtered.Count)
            return Common.Priority.None;

        // All the files must have a section that ends with .fx marking them as likely a reshade preset
        if (!iniData.All(f => f.Sections.All(x => x.EndsWith(".fx", StringComparison.CurrentCultureIgnoreCase))))
            return Common.Priority.None;

        return Common.Priority.Low;
    }

    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var game = installation.Game as AGame;
        var folder = game!.PrimaryFile.Path;
        foreach (var file in files.Where(f => !_ignoreFiles.Contains(f.Key.FileName)))
        {
            yield return new FromArchive
            {
                Id = ModFileId.New(),
                To = new GamePath(GameFolderType.Game, folder.Join(file.Key.FileName)),
                From = new HashRelativePath(srcArchive, file.Key),
                Hash = file.Value.Hash,
                Size = file.Value.Size,
                Store = file.Value.Store
            };
        }
    }
}
