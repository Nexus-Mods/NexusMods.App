using System.Net;
using FomodInstaller.Interface;
using FomodInstaller.Scripting.XmlScript;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
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
    private readonly IDataStore _dataStore;
    private readonly ICoreDelegates _delegates;
    private readonly XmlScriptType _scriptType = new();
    private readonly ILogger<FomodXmlInstaller> _logger;

    public FomodXmlInstaller(IDataStore dataStore, ILogger<FomodXmlInstaller> logger, ICoreDelegates coreDelegates)
    {
        _dataStore = dataStore;
        _delegates = coreDelegates;
        _logger = logger;
    }

    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
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
        // the component dealing with fomods is built to support all kinds of mods, including those without a script.
        // for those cases, stop patterns can be way more complex to deduce the intended installation structure. In our case, where
        // we only intend to support xml scripted fomods, this should be good enough
        var stopPattern = new List<string> { "fomod" };

        var xmlFileList = archiveFiles.Keys.Where(x => x.EndsWith(FomodConstants.XmlConfigRelativePath));
        var xmlCount = xmlFileList.Count();
        if (xmlCount != 1)
            throw new Exception($"$[{nameof(FomodXmlInstaller)}] found $[{xmlCount}] fomod configuration files. This should never happen and is indicative of a bug.");
        var xmlFile = xmlFileList.First();
        
        if (!archiveFiles.TryGetValue(xmlFile, out var analyzedFile))
            throw new Exception($"$[{nameof(FomodXmlInstaller)}] XML not found. This should never be true and is indicative of a bug.");

        var analyzerInfo = analyzedFile.AnalysisData.OfType<FomodAnalyzerInfo>().FirstOrDefault();
        if (analyzerInfo == default)
            return Array.Empty<ModInstallerResult>(); // <= invalid FOMOD, so no analyzer info dumped

        // Setup mod, exclude script path so it doesn't get picked up and thus double read from disk
        var modFiles = archiveFiles.Keys.Select(x => x.ToString()).ToList();
        var mod = new Mod(modFiles, stopPattern, xmlFile, string.Empty, _scriptType);
        await mod.InitializeWithoutLoadingScript();

        var executor = _scriptType.CreateExecutor(mod, _delegates);
        var installScript = _scriptType.LoadScript(analyzerInfo.XmlScript, true);
        var instructions = await executor.Execute(installScript, "", null);

        var errors = instructions.Where(_ => _.type == "error");
        if (errors.Any())
            throw new Exception(string.Join("; ", errors.Select(_ => _.source)));

        // I don't think this can happen on xml installers, afaik this would only happen on c# scripts that
        // try to directly change the plugin load order
        foreach (var warning in instructions.Where(_ => _.type == "unsupported"))
            _logger.LogWarning("Installer uses unsupported function: {}", warning.source);

        return new[]
        {
            new ModInstallerResult
            {
                Id = baseModId,
                Files = InstructionsToModFiles(instructions, srcArchiveHash, archiveFiles)
            }
        };
    }

    private IEnumerable<AModFile> InstructionsToModFiles(IEnumerable<Instruction> instructions, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var groupedInstructions = instructions.Aggregate(new Dictionary<string, List<Instruction>>(), (prev, iter) => {
            SetDefault(prev, iter.type, new List<Instruction>()).Add(iter);
            return prev;
        });

        var result = new List<AModFile>();
        foreach (var type in groupedInstructions)
            result.AddRange(ConvertInstructions(type.Value, srcArchive, files));

        return result;
    }

    private IEnumerable<AModFile> ConvertInstructions(IEnumerable<Instruction> instructions, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var result = new List<AModFile>();
        if (!instructions.Any())
            return result;
    
        instructions.GroupBy(instruction => instruction.type).ToList().ForEach(instructionGroup =>
        {
            IEnumerable<Instruction> instructionList = instructionGroup.ToList();
            if (instructionGroup.Any())
            {
                switch (instructionGroup.First().type)
                {
                    case "copy": result.AddRange(ConvertInstructionCopy(instructionList, srcArchive, files));
                        break;
                    case "mkdir": result.AddRange(ConvertInstructionMkdir(instructionList));
                        break;
                    case "enableallplugins": result.AddRange(ConvertInstructionEnableAllPlugins(instructionList));
                        break;
                    default:
                        _logger.LogError("Unsupported instruction type: {}", instructionList.First().type);
                        break;
                    // "iniedit" - only supported by c# script and modscript installers atm
                    // "generatefile" - only supported by c# script installers
                    // "enableplugin" - supported in the fomod-installer module but doesn't seem to be emitted anywhere
                } 
            }
        });

        return new List<AModFile>();
    }

    private IEnumerable<AModFile> ConvertInstructionCopy(IEnumerable<Instruction> instructions, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        return instructions.Select(instruction =>
        {
            var file = files.First(_ => _.Key.Equals((RelativePath)instruction.source));

            return new FromArchive
            {
                Id = ModFileId.New(),
                To = new GamePath(GameFolderType.Game, instruction.destination),
                Hash = file.Value.Hash,
                Size = file.Value.Size
            };
        });
    }

    private IEnumerable<AModFile> ConvertInstructionMkdir(IEnumerable<Instruction> instructions)
    {
        return instructions.Select(instruction => new EmptyDirectory
        {
            Id = ModFileId.New(),
            Directory = new GamePath(GameFolderType.Game, instruction.destination)
        });
    }

    private IEnumerable<AModFile> ConvertInstructionEnableAllPlugins(IEnumerable<Instruction> instruction)
    {
        return new List<AModFile>();
    }

    private TValue SetDefault<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key, TValue def) where TValue : class?
    {
        if (!dict.ContainsKey(key))
            dict.Add(key, def);

        return dict[key];
    }
}
