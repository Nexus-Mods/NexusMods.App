using FomodInstaller.Interface;
using FomodInstaller.Scripting.XmlScript;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.FOMOD.CoreDelegates;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Paths.Utilities;
using IFileSystem = NexusMods.Paths.IFileSystem;
using FomodMod = FomodInstaller.Interface.Mod;

namespace NexusMods.Games.FOMOD;

public class FomodXmlInstaller : ALibraryArchiveInstaller
{
    private readonly ICoreDelegates _delegates;
    private readonly XmlScriptType _scriptType = new();
    private readonly ILogger<FomodXmlInstaller> _logger;
    private readonly GamePath _fomodInstallationPath;
    private readonly IFileSystem _fileSystem;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IFileStore _fileStore;

    /// <summary>
    /// Creates a new instance of <see cref="FomodXmlInstaller"/> given the provided <paramref name="provider"/> and <paramref name="fomodInstallationPath"/>.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="fomodInstallationPath"></param>
    /// <returns></returns>
    public static FomodXmlInstaller Create(IServiceProvider provider, GamePath fomodInstallationPath)
    {
        return new FomodXmlInstaller(provider, provider.GetRequiredService<ILogger<FomodXmlInstaller>>(), fomodInstallationPath);
    }

    private FomodXmlInstaller(
        IServiceProvider serviceProvider,
        ILogger<FomodXmlInstaller> logger,
        GamePath fomodInstallationPath) : base(serviceProvider, logger)
    {
        _logger = logger;
        _fomodInstallationPath = fomodInstallationPath;

        _delegates = serviceProvider.GetRequiredService<ICoreDelegates>();
        _temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _fileStore = serviceProvider.GetRequiredService<IFileStore>();
    }

    public override async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(libraryArchive, loadoutGroup, transaction, loadout, null, cancellationToken);
    }

    public async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        FomodOption[]? options,
        CancellationToken cancellationToken)
    {
        // the component dealing with FOMODs is built to support all kinds of mods, including those without a script.
        // for those cases, stop patterns can be way more complex to deduce the intended installation structure. In our case, where
        // we only intend to support xml scripted FOMODs, this should be good enough
        var stopPattern = new List<string> { "fomod" };

        var tree = LibraryArchiveTree.Create(libraryArchive);

        var analyzerInfo = await FomodAnalyzer.AnalyzeAsync(tree, _fileSystem, _fileStore,cancellationToken);
        if (analyzerInfo is null) return new NotSupported(Reason: "Found no FOMOD data in the archive");

        await using var tmpFolder = _temporaryFileManager.CreateFolder();
        await analyzerInfo.DumpToFileSystemAsync(tmpFolder.Path.Combine(analyzerInfo.PathPrefix));

        var xmlPath = analyzerInfo.PathPrefix.Join(FomodConstants.XmlConfigRelativePath);

        var files = tree.GetFiles().Select(x => x.Item.Path.ToNativeSeparators(OSInformation.Shared)).ToList();
        var mod = new FomodMod(files, stopPattern, xmlPath.ToString(), string.Empty, _scriptType);
        await mod.InitializeWithoutLoadingScript();

        // NOTE(erri120): The FOMOD library calls us, so this is the only way we can pass data along.
        var installerDelegates = _delegates as InstallerDelegates;
        if (installerDelegates is not null)
        {
            if (options is not null)
            {
                // NOTE(halgari) The support for passing in presets to the installer is utterly broken. So we're going to
                // something different: we will create a new guided installer that will simply emit the user choices based on the
                // provided options.
                var installer = new PresetGuidedInstaller(options);
                installerDelegates.UiDelegates = new UiDelegates(ServiceProvider.GetRequiredService<ILogger<UiDelegates>>(), installer);
            }
            installerDelegates.UiDelegates.CurrentArchiveFiles = tree;
        }

        var executor = _scriptType.CreateExecutor(mod, _delegates);
        var installScript = _scriptType.LoadScript(FixXmlScript(analyzerInfo.XmlScript), true);
        var instructions = await executor.Execute(installScript, "", null);

        // NOTE(err120): Reset the previously provided data
        if (installerDelegates is not null)
            installerDelegates.UiDelegates.CurrentArchiveFiles = tree;

        var errors = instructions.Where(instruction => instruction.type == "error").ToArray();
        if (errors.Length != 0) throw new Exception(string.Join("; ", errors.Select(err => err.source)));

        // I don't think this can happen on xml installers, afaik this would only happen on c# scripts that
        // try to directly change the plugin load order
        foreach (var warning in instructions.Where(instruction => instruction.type == "unsupported"))
            _logger.LogWarning("Installer uses unsupported function: {}", warning.source);

        InstructionsToLoadoutItems(transaction, loadout, loadoutGroup,instructions, tree, _fomodInstallationPath);
        return new Success();
    }

    private static string FixXmlScript(string input)
    {
        // NOTE(erri120): The FOMOD library we're using does some really funky path normalization.
        // These don't really work well with our internal path representation and on systems
        // where the main directory separator character is the forward slash.
        // See https://github.com/Nexus-Mods/NexusMods.App/issues/625 for details.
        return Path.DirectorySeparatorChar == PathHelpers.DirectorySeparatorChar ? input.Replace('\\', PathHelpers.DirectorySeparatorChar) : input;
    }

    private void InstructionsToLoadoutItems(
        ITransaction transaction,
        LoadoutId loadoutId,
        LoadoutItemGroup.New loadoutGroup,
        IList<Instruction> instructions,
        KeyedBox<RelativePath, LibraryArchiveTree> tree,
        GamePath gamePath)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.type == "copy")
            {
                ConvertInstructionCopy(transaction, instruction, loadoutGroup, loadoutId, tree, gamePath);
            }
            // TODO: "mkdir" - not sure if we need/want this
            // TODO: "enableallplugins"
            // "iniedit" - only supported by c# script and modscript installers atm
            // "generatefile" - only supported by c# script installers
            // "enableplugin" - supported in the fomod-installer module but doesn't seem to be emitted anywhere
            else
            {
                _logger.LogWarning("Unknown FOMOD instruction type: {Type}", instruction.type);
            }
        }
    }

    private static void ConvertInstructionCopy(
        ITransaction transaction,
        Instruction instruction,
        LoadoutItemGroup.New loadoutGroup,
        LoadoutId loadoutId,
        KeyedBox<RelativePath, LibraryArchiveTree> tree,
        GamePath gamePath)
    {
        var src = RelativePath.FromUnsanitizedInput(instruction.source);
        var dest = RelativePath.FromUnsanitizedInput(instruction.destination);

        var file = tree.FindByPathFromChild(src);
        if (file is null) throw new KeyNotFoundException($"Unable to find file `{src}` in tree!");

        _ = file.ToLoadoutFile(loadoutId, loadoutGroup, transaction, new GamePath(gamePath.LocationId, gamePath.Path.Join(dest)));
    }
}
