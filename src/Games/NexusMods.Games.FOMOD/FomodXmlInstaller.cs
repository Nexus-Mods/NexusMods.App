using System.Diagnostics;
using FomodInstaller.Interface;
using FomodInstaller.Scripting.XmlScript;
using Microsoft.Extensions.DependencyInjection;
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
using OneOf.Types;
using Mod = FomodInstaller.Interface.Mod;

namespace NexusMods.Games.FOMOD;

public class FomodXmlInstaller : IModInstaller
{
    private readonly ICoreDelegates _delegates;
    private readonly XmlScriptType _scriptType = new();
    private readonly ILogger<FomodXmlInstaller> _logger;
    private readonly GamePath _fomodInstallationPath;

    /// <summary>
    /// Creates a new instance of <see cref="FomodXmlInstaller"/> given the provided <paramref name="provider"/> and <paramref name="fomodInstallationPath"/>.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="fomodInstallationPath"></param>
    /// <returns></returns>
    public static FomodXmlInstaller Create(IServiceProvider provider, GamePath fomodInstallationPath)
    {
        return new FomodXmlInstaller(provider.GetRequiredService<ILogger<FomodXmlInstaller>>(),
            provider.GetRequiredService<ICoreDelegates>(),
            fomodInstallationPath);
    }

    public FomodXmlInstaller(ILogger<FomodXmlInstaller> logger, ICoreDelegates coreDelegates,
        GamePath fomodInstallationPath)
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
                Files = InstructionsToModFiles(instructions, archiveFiles, _fomodInstallationPath)
            }
        };
    }

    private IEnumerable<AModFile> InstructionsToModFiles(
        IList<Instruction> instructions,
        EntityDictionary<RelativePath, AnalyzedFile> files,
        GamePath gameTargetPath)
    {
        var res = instructions.Select(instruction =>
        {
            return instruction.type switch
            {
                "copy" => ConvertInstructionCopy(instruction, files, gameTargetPath),
                "mkdir" => ConvertInstructionMkdir(instruction, gameTargetPath),
                // TODO: "enableallplugins",
                // "iniedit" - only supported by c# script and modscript installers atm
                // "generatefile" - only supported by c# script installers
                // "enableplugin" - supported in the fomod-installer module but doesn't seem to be emitted anywhere
                _ => ReportUnknownType(instruction.type),
            };
        }).Where(x => x is not null).Select(x => x!).ToArray();

        return res;
    }

    private AModFile? ReportUnknownType(string instructionType)
    {
        _logger.LogWarning("Unknown FOMOD instruction type: {Type}", instructionType);
        return null;
    }

    private static AModFile ConvertInstructionCopy(
        Instruction instruction,
        EntityDictionary<RelativePath, AnalyzedFile> files,
        GamePath gameTargetPath)
    {
        var src = RelativePath.FromUnsanitizedInput(instruction.source);
        var dest = RelativePath.FromUnsanitizedInput(instruction.destination);

        var file = files[src];
        return new FromArchive
        {
            Id = ModFileId.New(),
            To = new GamePath(gameTargetPath.Type, gameTargetPath.Path.Join(dest)),
            Hash = file.Hash,
            Size = file.Size
        };
    }

    private static AModFile ConvertInstructionMkdir(
        Instruction instruction,
        GamePath gameTargetPath)
    {
        var dest = RelativePath.FromUnsanitizedInput(instruction.destination);
        return new EmptyDirectory
        {
            Id = ModFileId.New(),
            Directory = new GamePath(gameTargetPath.Type, gameTargetPath.Path.Join(dest))
        };
    }
}
