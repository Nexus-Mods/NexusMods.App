using System.Diagnostics;
using FomodInstaller.Interface;
using FomodInstaller.Scripting.XmlScript;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using Mod = FomodInstaller.Interface.Mod;

namespace NexusMods.Games.FOMOD;

public class FomodXmlInstaller : IModInstaller
{
    private readonly ICoreDelegates _delegates;
    private readonly XmlScriptType _scriptType = new();
    private readonly ILogger<FomodXmlInstaller> _logger;
    private readonly GamePath _fomodInstallationPath;

    public FomodXmlInstaller(ILogger<FomodXmlInstaller> logger, ICoreDelegates coreDelegates, GamePath fomodInstallationPath)
    {
        _delegates = coreDelegates;
        _fomodInstallationPath = fomodInstallationPath;
        _logger = logger;
    }

    public Priority GetPriority(GameInstallation installation,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        var hasScript = archiveFiles.Keys.Any(x => x.EndsWith(FomodConstants.XmlConfigRelativePath));
        return hasScript ? Priority.High : Priority.None;
    }

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        ModId baseModId,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
        CancellationToken cancellationToken = default)
    {

        // the component dealing with FOMODs is built to support all kinds of mods, including those without a script.
        // for those cases, stop patterns can be way more complex to deduce the intended installation structure. In our case, where
        // we only intend to support xml scripted FOMODs, this should be good enough
        var stopPattern = new List<string> { "fomod" };

        if (!archiveFiles.Keys.TryGetFirst(x => x.EndsWith(FomodConstants.XmlConfigRelativePath), out var xmlFile))
            throw new UnreachableException(
                $"$[{nameof(FomodXmlInstaller)}] XML file not found. This should never be true and is indicative of a bug.");

        if (!archiveFiles.TryGetValue(xmlFile, out var analyzedFile))
            throw new UnreachableException(
                $"$[{nameof(FomodXmlInstaller)}] XML data not found. This should never be true and is indicative of a bug.");

        var analyzerInfo = analyzedFile.AnalysisData.OfType<FomodAnalyzerInfo>().FirstOrDefault();
        if (analyzerInfo == default) return Array.Empty<ModInstallerResult>();

        // Setup mod, exclude script path so it doesn't get picked up and thus double read from disk
        var modFiles = archiveFiles.Keys.Select(x => x.ToString()).ToList();
        var mod = new Mod(modFiles, stopPattern, xmlFile, string.Empty, _scriptType);
        await mod.InitializeWithoutLoadingScript();

        var executor = _scriptType.CreateExecutor(mod, _delegates);
        var installScript = _scriptType.LoadScript(analyzerInfo.XmlScript, true);
        var instructions = await executor.Execute(installScript, "", null);

        var errors = instructions.Where(instruction => instruction.type == "error").ToArray();
        if (errors.Any()) throw new Exception(string.Join("; ", errors.Select(err => err.source)));

        // I don't think this can happen on xml installers, afaik this would only happen on c# scripts that
        // try to directly change the plugin load order
        foreach (var warning in instructions.Where(instruction => instruction.type == "unsupported"))
            _logger.LogWarning("Installer uses unsupported function: {}", warning.source);

        return new[]
        {
            new ModInstallerResult
            {
                Id = baseModId,
                Files = InstructionsToModFiles(instructions, srcArchiveHash, archiveFiles, _fomodInstallationPath)
            }
        };
    }

    private IEnumerable<AModFile> InstructionsToModFiles(IEnumerable<Instruction> instructions, Hash srcArchive,
        EntityDictionary<RelativePath, AnalyzedFile> files, GamePath gameTargetPath)
    {
        var groupedInstructions = instructions.Aggregate(new Dictionary<string, List<Instruction>>(),
            (prev, instruction) =>
            {
                if (prev.TryGetValue(instruction.type, out var existing))
                {
                    existing.Add(instruction);
                }
                else
                {
                    prev[instruction.type] = new List<Instruction>
                    {
                        instruction
                    };
                }

                return prev;
            });

        var result = new List<AModFile>();
        foreach (var type in groupedInstructions)
            result.AddRange(ConvertInstructions(type.Value, files, gameTargetPath));

        return result;
    }

    private static IEnumerable<AModFile> ConvertInstructions(IList<Instruction> instructions,
        EntityDictionary<RelativePath, AnalyzedFile> files, GamePath gameTargetPath)
    {
        if (!instructions.Any()) return new List<AModFile>();

        return instructions.First().type switch
        {
            "copy" => ConvertInstructionCopy(instructions, files, gameTargetPath),
            "mkdir" => ConvertInstructionMkdir(instructions, gameTargetPath),
            // TODO: "enableallplugins",
            // "iniedit" - only supported by c# script and modscript installers atm
            // "generatefile" - only supported by c# script installers
            // "enableplugin" - supported in the fomod-installer module but doesn't seem to be emitted anywhere
            _ => new List<AModFile>()
        };
    }

    private static IEnumerable<AModFile> ConvertInstructionCopy(IEnumerable<Instruction> instructions,
        EntityDictionary<RelativePath, AnalyzedFile> files, GamePath gameTargetPath)
    {
        return instructions.Select(instruction =>
        {
            var file = files.First(file => file.Key.Equals(RelativePath.FromUnsanitizedInput(instruction.source)));

            return new FromArchive
            {
                Id = ModFileId.New(),
                To = new GamePath(gameTargetPath.Type, gameTargetPath.Path.Join(instruction.destination)),
                Hash = file.Value.Hash,
                Size = file.Value.Size
            };
        });
    }

    private static IEnumerable<AModFile> ConvertInstructionMkdir(IEnumerable<Instruction> instructions,
        GamePath gameTargetPath)
    {
        return instructions.Select(instruction => new EmptyDirectory
        {
            Id = ModFileId.New(),
            Directory = new GamePath(gameTargetPath.Type, gameTargetPath.Path.Join(instruction.destination))
        });
    }
}
