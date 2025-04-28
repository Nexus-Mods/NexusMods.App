using System.Diagnostics;
using FomodInstaller.Interface;
using FomodInstaller.Scripting;
using FomodInstaller.Scripting.XmlScript;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Extensions.BCL;
using NexusMods.Games.FOMOD.CoreDelegates;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
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
        if (!libraryArchive.Children.TryGetFirst(x => x.Path.EndsWith(FomodConstants.XmlConfigRelativePath), out var xmlFile))
            return new NotSupported(Reason: "Found no FOMOD data in the archive");

        // `foo/bar/baz/fomod/ModuleConfig.xml` -> `foo/bar/baz`
        // `fomod/ModuleConfig.xml` -> empty string
        var fomodPathPrefix = xmlFile.Path.Parent.Parent;
        var pathPrefixDropCount = fomodPathPrefix.Length == 0 ? 0 : fomodPathPrefix.Depth + 1;

        var fomodArchiveFiles = libraryArchive.Children
            .Where(x => x.Path.InFolder(fomodPathPrefix))
            .Select(x => new KeyValuePair<RelativePath, LibraryArchiveFileEntry.ReadOnly>(x.Path.DropFirst(pathPrefixDropCount), x))
            .DistinctBy(kv => kv.Key)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var mod = new FomodMod(
            listModFiles: fomodArchiveFiles.Keys.Select(static x => x.ToString()).ToList(),
            stopPatterns: FomodConstants.StopPattern,
            installScriptPath: xmlFile.Path.DropFirst(pathPrefixDropCount), 
            tempFolderPath: string.Empty,
            scriptType: _scriptType
        );

        // NOTE(erri120): We're loading the script manually, otherwise the FOMOD library will load the script from the file system
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

            installerDelegates.UiDelegates.CurrentFomodArchiveFiles = fomodArchiveFiles;
        }

        var rawScript = await LoadScript(xmlFile.AsLibraryFile().Hash, cancellationToken);

        var executor = _scriptType.CreateExecutor(mod, _delegates);
        var installScript = _scriptType.LoadScript(rawScript, true);
        FixScript(installScript, fomodArchiveFiles);

        var instructions = await executor.Execute(installScript, "", null);

        // NOTE(err120): Reset the previously provided data
        if (installerDelegates is not null) installerDelegates.UiDelegates.CurrentFomodArchiveFiles = fomodArchiveFiles;

        var errors = instructions.Where(instruction => instruction.type == "error").ToArray();
        if (errors.Length != 0) throw new Exception(string.Join("; ", errors.Select(err => err.source)));

        foreach (var warning in instructions.Where(instruction => instruction.type == "unsupported"))
            _logger.LogWarning("Installer uses unsupported function: {}", warning.source);

        InstructionsToLoadoutItems(transaction, loadout, loadoutGroup,instructions, fomodArchiveFiles, _fomodInstallationPath);
        return new Success();
    }

    private async ValueTask<string> LoadScript(Hash hash, CancellationToken cancellationToken = default)
    {
        await using var stream = await _fileStore.GetFileStream(hash, cancellationToken);
        using var streamReader = new StreamReader(stream);
        return await streamReader.ReadToEndAsync(cancellationToken);
    }

    private void FixScript(IScript script, Dictionary<RelativePath, LibraryArchiveFileEntry.ReadOnly> fomodArchiveFiles)
    {
        Debug.Assert(script is XmlScript);
        if (script is not XmlScript xmlScript) return;

        FixPaths(fomodArchiveFiles, xmlScript.RequiredInstallFiles);
        FixPaths(fomodArchiveFiles, xmlScript.ConditionallyInstalledFileSets.SelectMany(x => x.Files));

        foreach (var option in xmlScript.InstallSteps.SelectMany(x => x.OptionGroups).SelectMany(x => x.Options))
        {
            option.ImagePath = FixPath(option.ImagePath, fomodArchiveFiles);
            FixPaths(fomodArchiveFiles, option.Files);
        }
    }

    private void FixPaths(Dictionary<RelativePath, LibraryArchiveFileEntry.ReadOnly> fomodArchiveFiles, IEnumerable<InstallableFile> installableFiles)
    {
        foreach (var installableFile in installableFiles)
        {
            installableFile.Source = FixPath(installableFile.Source, fomodArchiveFiles, isDirectory: installableFile.IsFolder);
            installableFile.Destination = RelativePath.FromUnsanitizedInput(installableFile.Destination);
        }
    }

    private string FixPath(string? input, Dictionary<RelativePath, LibraryArchiveFileEntry.ReadOnly> fomodArchiveFiles, bool isDirectory = false)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var path = RelativePath.FromUnsanitizedInput(input);
        if (isDirectory || fomodArchiveFiles.ContainsKey(path)) return path.ToString();

        _logger.LogWarning("Didn't find matching archive file for referenced file in FOMOD `{OldPath}` -> `{NewPath}`", input, path);
        return input;

    }

    private void InstructionsToLoadoutItems(
        ITransaction transaction,
        LoadoutId loadoutId,
        LoadoutItemGroup.New loadoutGroup,
        IList<Instruction> instructions,
        Dictionary<RelativePath, LibraryArchiveFileEntry.ReadOnly> fomodArchiveFiles,
        GamePath gamePath)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.type == "copy")
            {
                ConvertInstructionCopy(transaction, instruction, loadoutGroup, loadoutId, fomodArchiveFiles, gamePath);
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

    private void ConvertInstructionCopy(
        ITransaction transaction,
        Instruction instruction,
        LoadoutItemGroup.New loadoutGroup,
        LoadoutId loadoutId,
        Dictionary<RelativePath, LibraryArchiveFileEntry.ReadOnly> fomodArchiveFiles,
        GamePath gamePath)
    {
        var src = RelativePath.FromUnsanitizedInput(instruction.source);
        var dest = RelativePath.FromUnsanitizedInput(instruction.destination);

        if (!fomodArchiveFiles.TryGetValue(src, out var libraryArchiveFile))
        {
            _logger.LogError("Didn't find source file `{Path}` in FOMOD archive", src);
            return;
        }

        _ = new LoadoutFile.New(transaction, out var id)
        {
            LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, id)
            {
                LoadoutItem = new LoadoutItem.New(transaction, id)
                {
                    LoadoutId = loadoutId,
                    Name = libraryArchiveFile.Path.FileName,
                    ParentId = loadoutGroup,
                },
                TargetPath = new GamePath(gamePath.LocationId, gamePath.Path.Join(dest)).ToGamePathParentTuple(loadoutId),
            },
            Hash = libraryArchiveFile.AsLibraryFile().Hash,
            Size = libraryArchiveFile.AsLibraryFile().Size,
        };
    }
}
