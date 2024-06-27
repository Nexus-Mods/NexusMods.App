using FomodInstaller.Interface;
using FomodInstaller.Scripting.XmlScript;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.FOMOD.CoreDelegates;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Paths.Utilities;
using File = NexusMods.Abstractions.Loadouts.Files.File;
using IFileSystem = NexusMods.Paths.IFileSystem;
using Mod = FomodInstaller.Interface.Mod;

namespace NexusMods.Games.FOMOD;

public class FomodXmlInstaller : AModInstaller
{
    private readonly ICoreDelegates _delegates;
    private readonly XmlScriptType _scriptType = new();
    private readonly ILogger<FomodXmlInstaller> _logger;
    private readonly GamePath _fomodInstallationPath;
    private readonly IFileSystem _fileSystem;
    private readonly TemporaryFileManager _temporaryFileManager;

    /// <summary>
    /// Creates a new instance of <see cref="FomodXmlInstaller"/> given the provided <paramref name="provider"/> and <paramref name="fomodInstallationPath"/>.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="fomodInstallationPath"></param>
    /// <returns></returns>
    public static FomodXmlInstaller Create(IServiceProvider provider, GamePath fomodInstallationPath)
    {
        return new FomodXmlInstaller(provider.GetRequiredService<ILogger<FomodXmlInstaller>>(),
            provider.GetRequiredService<ICoreDelegates>(), provider.GetRequiredService<IFileSystem>(),
            provider.GetRequiredService<TemporaryFileManager>(),

            fomodInstallationPath, provider);
    }

    public FomodXmlInstaller(ILogger<FomodXmlInstaller> logger, ICoreDelegates coreDelegates,
        IFileSystem fileSystem, TemporaryFileManager temporaryFileManager, GamePath fomodInstallationPath,
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _delegates = coreDelegates;
        _fomodInstallationPath = fomodInstallationPath;
        _logger = logger;
        _temporaryFileManager = temporaryFileManager;
        _fileSystem = fileSystem;
    }

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        // the component dealing with FOMODs is built to support all kinds of mods, including those without a script.
        // for those cases, stop patterns can be way more complex to deduce the intended installation structure. In our case, where
        // we only intend to support xml scripted FOMODs, this should be good enough
        var stopPattern = new List<string> { "fomod" };

        var analyzerInfo = await FomodAnalyzer.AnalyzeAsync(info.ArchiveFiles, _fileSystem, cancellationToken);
        if (analyzerInfo == default)
            return Array.Empty<ModInstallerResult>();

        await using var tmpFolder = _temporaryFileManager.CreateFolder();
        await analyzerInfo.DumpToFileSystemAsync(tmpFolder.Path.Combine(analyzerInfo.PathPrefix));

        var xmlPath = analyzerInfo.PathPrefix.Join(FomodConstants.XmlConfigRelativePath);

        // Setup mod, exclude script path so it doesn't get picked up and thus double read from disk
        var modFiles = info.ArchiveFiles.GetFiles().Select(x => x.Path().ToNativeSeparators(OSInformation.Shared)).ToList();
        var mod = new Mod(modFiles, stopPattern, xmlPath.ToString(), string.Empty, _scriptType);
        await mod.InitializeWithoutLoadingScript();

        // NOTE(erri120): The FOMOD library calls us, so this is the only way we can pass data along.
        var installerDelegates = _delegates as InstallerDelegates;
        if (installerDelegates is not null)
        {
            installerDelegates.UiDelegates.CurrentArchiveFiles = info.ArchiveFiles;
        }

        var executor = _scriptType.CreateExecutor(mod, _delegates);
        var installScript = _scriptType.LoadScript(FixXmlScript(analyzerInfo.XmlScript), true);
        var instructions = await executor.Execute(installScript, "", null);

        // NOTE(err120): Reset the previously provided data
        if (installerDelegates is not null)
            installerDelegates.UiDelegates.CurrentArchiveFiles = info.ArchiveFiles;

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
                Id = info.BaseModId,
                Files = InstructionsToModFiles(instructions, info.ArchiveFiles, _fomodInstallationPath)
            }
        };
    }

    private static string FixXmlScript(string input)
    {
        // NOTE(erri120): The FOMOD library we're using does some really funky path normalization.
        // These don't really work well with our internal path representation and on systems
        // where the main directory separator character is the forward slash.
        // See https://github.com/Nexus-Mods/NexusMods.App/issues/625 for details.
        return Path.DirectorySeparatorChar == PathHelpers.DirectorySeparatorChar ? input.Replace('\\', PathHelpers.DirectorySeparatorChar) : input;
    }

    private IEnumerable<TempEntity> InstructionsToModFiles(
        IList<Instruction> instructions,
        KeyedBox<RelativePath, ModFileTree> files,
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

    private TempEntity? ReportUnknownType(string instructionType)
    {
        _logger.LogWarning("Unknown FOMOD instruction type: {Type}", instructionType);
        return null;
    }

    private static TempEntity ConvertInstructionCopy(
        Instruction instruction,
        KeyedBox<RelativePath, ModFileTree> files,
        GamePath gameTargetPath)
    {
        var src = RelativePath.FromUnsanitizedInput(instruction.source);
        var dest = RelativePath.FromUnsanitizedInput(instruction.destination);

        var file = files.FindByPathFromChild(src)!;
        return new TempEntity
        {
            { StoredFile.Hash, file.Item.Hash },
            { StoredFile.Size, file.Item.Size },
            { File.To, new GamePath(gameTargetPath.LocationId, gameTargetPath.Path.Join(dest)) },
        };
    }


    private static TempEntity ConvertInstructionMkdir(
        Instruction instruction,
        GamePath gameTargetPath)
    {
        var dest = RelativePath.FromUnsanitizedInput(instruction.destination);
        return new TempEntity
        {
            { File.To, new GamePath(gameTargetPath.LocationId, gameTargetPath.Path.Join(dest)) },
            { EmptyDirectory.Directory, true }
        };
    }
}
