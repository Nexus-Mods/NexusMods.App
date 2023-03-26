using FomodInstaller.Interface;
using FomodInstaller.Scripting.XmlScript;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using System;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using static System.Collections.Specialized.BitVector32;
using Mod = FomodInstaller.Interface.Mod;

namespace NexusMods.FOMOD;

public class FomodXMLInstaller : IModInstaller
{
    private readonly ICoreDelegates _delegates;
    private readonly IDataStore _store;
    private readonly XmlScriptType _scriptType = new XmlScriptType();
    private readonly TemporaryFileManager _tmpFiles;
    private readonly FileExtractor.FileExtractor _extractor;
    private readonly ILogger<FomodXMLInstaller> _logger;

    public FomodXMLInstaller(ILogger<FomodXMLInstaller> logger, IDataStore store, TemporaryFileManager tmpFiles, FileExtractor.FileExtractor extractor, ICoreDelegates coreDelegates)
    {
        _delegates = coreDelegates;
        _store = store;
        _logger = logger;
        _tmpFiles = tmpFiles;
        _extractor = extractor;
    }

    public async Task<IEnumerable<AModFile>> Install(GameInstallation installation, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files, CancellationToken cancel)
    {
        var filePaths = files.Select(_ => (string)_.Key).ToList();
        // the component dealing with fomods is built to support all kinds of mods, including those without a script.
        // for those cases, stop patterns can be way more complex to deduce the intended installation structure. In our case, where
        // we only intend to support xml scripted fomods, this should be good enough
        var stopPattern = new List<string> { "fomod" };

        var found = _store.Get<AnalyzedFile>(new Id64(EntityCategory.FileAnalysis, (ulong)srcArchive));

        if (found is not AnalyzedArchive archive)
        {
            throw new Exception($"expected installation to be on an archive");
        }


        await using var tmpFolder = _tmpFiles.CreateFolder();
        // @todo redundant, the archive analyzation already extracted the archive but then immediately deleted the files, that
        //   needs to be changed to support any installer that needs to access file content
        await _extractor.ExtractAllAsync(found.SourcePath, tmpFolder, cancel);


        var mod = new Mod(filePaths, stopPattern, "fomod/ModuleConfig.xml", tmpFolder.Path.ToString(), _scriptType);
        await mod.Initialize(true);
        var executor = _scriptType.CreateExecutor(mod, _delegates);
        string dataPath = "";
        var installScript = _scriptType.LoadScript(await AbsolutePath.FromFullPath(Path.Join(tmpFolder.Path.ToString(), "fomod", "ModuleConfig.xml")).ReadAllTextAsync(cancel), false);
        IList<Instruction> instructions = await executor.Execute(installScript, dataPath, null);

        var errors = instructions.Where(_ => _.type == "error");
        if (errors.Count() > 0)
        {
            throw new Exception(string.Join("; ", errors.Select(_ => _.source)));
        }

        // I don't think this can happen on xml installers, afaik this would only happen on c# scripts that
        // try to directly change the plugin load order
        foreach (var warning in instructions.Where(_ => _.type == "unsupported"))
        {
            _logger.LogWarning("Installer uses unsupported function: {}", warning.source);
        }

        return InstructionsToModFiles(instructions, srcArchive, files);
    }

    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var hasScript = files.Any(kv =>
            (kv.Key.Depth > 1)
            && ((string)kv.Key.Parent.FileName).Equals("fomod", StringComparison.InvariantCultureIgnoreCase)
            && ((string)kv.Key.FileName).Equals("ModuleConfig.xml", StringComparison.InvariantCultureIgnoreCase));

        return hasScript ? Common.Priority.High : Common.Priority.None;
    }

    private IEnumerable<AModFile> InstructionsToModFiles(IEnumerable<Instruction> instructions, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        var groupedInstructions = instructions.Aggregate(new Dictionary<string, List<Instruction>>(), (prev, iter) => {
            SetDefault(prev, iter.type, new List<Instruction>()).Add(iter);
            return prev;
        });

        List<AModFile> result = new List<AModFile>();

        foreach (var type in groupedInstructions)
        {
            result.AddRange(ConvertInstructions(type.Value, srcArchive, files));
        }

        return result;
    }

    private IEnumerable<AModFile> ConvertInstructions(IEnumerable<Instruction> instructions, Hash srcArchive, EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        if (instructions.Any())
        {
            switch (instructions.First().type)
            {
                case "copy": return ConvertInstructionCopy(instructions, srcArchive, files);
                case "mkdir": return ConvertInstructionMkdir(instructions);
                case "enableallplugins": return ConvertInstructionEnableAllPlugins(instructions);
                // "iniedit" - only supported by c# script and modscript installers atm
                // "generatefile" - only supported by c# script installers
                // "enableplugin" - supported in the fomod-installer module but doesn't seem to be emitted anywhere
            }
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
                Size = file.Value.Size,
            };
        });
    }

    private IEnumerable<AModFile> ConvertInstructionMkdir(IEnumerable<Instruction> instructions)
    {
        return instructions.Select(instruction => new EmptyDirectory
        {
            Id = ModFileId.New(),
            To = new GamePath(GameFolderType.Game, instruction.destination),
        });
    }

    private IEnumerable<AModFile> ConvertInstructionEnableAllPlugins(IEnumerable<Instruction> instruction)
    {
        return new List<AModFile>();
    }

    private TValue SetDefault<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key, TValue def) where TValue : class?
    {
        if (!dict.ContainsKey(key))
        {
            dict.Add(key, def);
        }
        return dict[key];
    }
}
