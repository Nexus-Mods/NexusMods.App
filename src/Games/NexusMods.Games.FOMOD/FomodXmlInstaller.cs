using FomodInstaller.Interface;
using FomodInstaller.Scripting.XmlScript;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
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
    private readonly IDataStore _store;
    private readonly XmlScriptType _scriptType = new();
    private readonly TemporaryFileManager _tmpFiles;
    private readonly FileExtractor.FileExtractor _extractor;
    private readonly ILogger<FomodXmlInstaller> _logger;

    public FomodXmlInstaller(ILogger<FomodXmlInstaller> logger, IDataStore store, TemporaryFileManager tmpFiles, FileExtractor.FileExtractor extractor, ICoreDelegates coreDelegates)
    {
        _delegates = coreDelegates;
        _store = store;
        _logger = logger;
        _tmpFiles = tmpFiles;
        _extractor = extractor;
    }

    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var hasScript = files.ContainsKey(FomodConstants.XmlConfigRelativePath);
        return hasScript ? Common.Priority.High : Common.Priority.None;
    }

    public async ValueTask<IEnumerable<AModFile>> GetFilesToExtractAsync(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files, CancellationToken token)
    {
        // the component dealing with fomods is built to support all kinds of mods, including those without a script.
        // for those cases, stop patterns can be way more complex to deduce the intended installation structure. In our case, where
        // we only intend to support xml scripted fomods, this should be good enough
        var stopPattern = new List<string> { "fomod" };

        if (!files.TryGetValue(FomodConstants.XmlConfigRelativePath, out var analyzedFile))
            throw new Exception($"$[{nameof(FomodXmlInstaller)}] XML not found. This should never be true and is indicative of a bug.");

        var analyzerInfo = analyzedFile.AnalysisData.OfType<FomodAnalyzerInfo>().FirstOrDefault();
        if (analyzerInfo == default)
            return Array.Empty<AModFile>(); // <= invalid FOMOD, so no analyzer info dumped

        // Setup mod, exclude script path so it doesn't get picked up and thus double read from disk
        var modFiles = files.Keys.Select(x => x.ToString()).ToList();
        var mod = new Mod(modFiles, stopPattern, FomodConstants.XmlConfigRelativePath, string.Empty, _scriptType);
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

        return InstructionsToModFiles(instructions, srcArchive, files);
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
        if (!instructions.Any())
            return new List<AModFile>();

        switch (instructions.First().type)
        {
            case "copy": return ConvertInstructionCopy(instructions, srcArchive, files);
            case "mkdir": return ConvertInstructionMkdir(instructions);
            case "enableallplugins": return ConvertInstructionEnableAllPlugins(instructions);
            // "iniedit" - only supported by c# script and modscript installers atm
            // "generatefile" - only supported by c# script installers
            // "enableplugin" - supported in the fomod-installer module but doesn't seem to be emitted anywhere
        }

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
                From = new HashRelativePath(srcArchive, (RelativePath)instruction.source),
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
            To = new GamePath(GameFolderType.Game, instruction.destination)
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
