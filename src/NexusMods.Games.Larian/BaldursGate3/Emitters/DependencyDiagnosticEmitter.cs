using System.Runtime.CompilerServices;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Games.Larian.BaldursGate3.Utils.LsxXmlParsing;
using NexusMods.Hashing.xxHash3;
using NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;
using NexusMods.Paths;
using Polly;

namespace NexusMods.Games.Larian.BaldursGate3.Emitters;

public class DependencyDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger _logger;
    private readonly IResourceLoader<Hash, Outcome<LspkPackageFormat.PakMetaData>> _metadataPipeline;
    private readonly IOSInformation _os;

    public DependencyDiagnosticEmitter(IServiceProvider serviceProvider, ILogger<DependencyDiagnosticEmitter> logger, IOSInformation os)
    {
        _logger = logger;
        _metadataPipeline = Pipelines.GetMetadataPipeline(serviceProvider);
        _os = os;
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var bg3LoadoutFile = GetScriptExtenderLoadoutFile(loadout);
        
        // BG3SE WINEDLLOVERRIDE diagnostic
        if (_os.IsLinux  && bg3LoadoutFile.HasValue && loadout.InstallationInstance.LocatorResultMetadata is SteamLocatorResultMetadata)
        {
            // yield return Diagnostics.
            yield return Diagnostics.CreateBg3SeWineDllOverrideSteam(Template: "text");
        }
        
        var diagnostics = await DiagnosePakModulesAsync(loadout, bg3LoadoutFile, cancellationToken);
        foreach (var diagnostic in diagnostics)
        {
            yield return diagnostic;
        }
    }

#region Diagnosers

    private async Task<IEnumerable<Diagnostic>> DiagnosePakModulesAsync(Loadout.ReadOnly loadout, Optional<LoadoutFile.ReadOnly> bg3LoadoutFile, CancellationToken cancellationToken)
    {
        var pakLoadoutFiles = GetAllPakLoadoutFiles(loadout, onlyEnabledMods: true);
        var metaFileTuples = await GetAllPakMetadata(pakLoadoutFiles,
                _metadataPipeline,
                _logger,
                cancellationToken
            )
            .ToArrayAsync(cancellationToken: cancellationToken);

        var diagnostics = new List<Diagnostic>();

        foreach (var metaFileTuple in metaFileTuples)
        {
            var (mod, metadataOrError) = metaFileTuple;
            var loadoutItemGroup = mod.AsLoadoutItemWithTargetPath().AsLoadoutItem().Parent;

            // error case
            if (metadataOrError.Exception is not null)
            {
                diagnostics.Add(Diagnostics.CreateInvalidPakFile(
                        ModName: loadoutItemGroup.ToReference(loadout),
                        PakFileName: mod.AsLoadoutItemWithTargetPath().TargetPath.Item3.FileName
                    )
                );
                continue;
            }

            // non error case
            var pakMetaData = metadataOrError.Result;

            // check if the mod requires the script extender and if SE is missing
            if (pakMetaData.ScriptExtenderConfigMetadata is { HasValue: true, Value.RequiresScriptExtender: true } &&
                !bg3LoadoutFile.HasValue)
            {
                diagnostics.Add(Diagnostics.CreateMissingRequiredScriptExtender(
                    ModName: loadoutItemGroup.ToReference(loadout),
                    PakName:pakMetaData.MetaFileData.ModuleShortDesc.Name,
                    BG3SENexusLink:BG3SENexusModsLink));
            }


            // check dependencies
            var dependencies = pakMetaData.MetaFileData.Dependencies;
            foreach (var dependency in dependencies)
            {
                var dependencyUuid = dependency.Uuid;
                if (dependencyUuid == string.Empty)
                    continue;

                var matchingDeps = metaFileTuples.Where(
                        x =>
                            x.Item2.Exception is null &&
                            x.Item2.Result.MetaFileData.ModuleShortDesc.Uuid == dependencyUuid
                    )
                    .ToArray();

                if (matchingDeps.Length == 0)
                {
                    // Missing dependency
                    diagnostics.Add(Diagnostics.CreateMissingDependency(
                            ModName: loadoutItemGroup.ToReference(loadout),
                            MissingDepName: dependency.Name,
                            MissingDepVersion: dependency.SemanticVersion.ToString(),
                            PakModuleName: pakMetaData.MetaFileData.ModuleShortDesc.Name,
                            PakModuleVersion: pakMetaData.MetaFileData.ModuleShortDesc.SemanticVersion.ToString(),
                            NexusModsLink: NexusModsLink
                        )
                    );
                    continue;
                }

                if (dependency.SemanticVersion == default(LsxXmlFormat.ModuleVersion))
                    continue;

                var highestInstalledMatch = matchingDeps.MaxBy(
                    x => x.Item2.Result.MetaFileData.ModuleShortDesc.SemanticVersion
                );
                var installedMatchModule = highestInstalledMatch.Item2.Result.MetaFileData.ModuleShortDesc;
                var matchLoadoutItemGroup = highestInstalledMatch.Item1.AsLoadoutItemWithTargetPath().AsLoadoutItem().Parent;

                // Check if found dependency is outdated
                if (installedMatchModule.SemanticVersion < dependency.SemanticVersion)
                {
                    diagnostics.Add(Diagnostics.CreateOutdatedDependency(
                            ModName: loadoutItemGroup.ToReference(loadout),
                            PakModuleName: pakMetaData.MetaFileData.ModuleShortDesc.Name,
                            PakModuleVersion: pakMetaData.MetaFileData.ModuleShortDesc.SemanticVersion.ToString(),
                            DepModName: matchLoadoutItemGroup.ToReference(loadout),
                            DepName: installedMatchModule.Name,
                            MinDepVersion: dependency.SemanticVersion.ToString(),
                            CurrentDepVersion: installedMatchModule.SemanticVersion.ToString()
                        )
                    );
                }
            }
        }

        return diagnostics;
    }

#endregion Diagnosers

#region Helpers

    private static Optional<LoadoutFile.ReadOnly> GetScriptExtenderLoadoutFile(Loadout.ReadOnly loadout)
    {
        return loadout.Items.OfTypeLoadoutItemGroup()
            .Where(g => g.AsLoadoutItem().IsEnabled())
            .SelectMany(group => group.Children.OfTypeLoadoutItemWithTargetPath()
                .OfTypeLoadoutFile()
                .Where(file => file.AsLoadoutItemWithTargetPath().TargetPath.Item2 == Bg3Constants.BG3SEGamePath.LocationId &&
                               file.AsLoadoutItemWithTargetPath().TargetPath.Item3 == Bg3Constants.BG3SEGamePath.Path
                )
            ).FirstOrOptional(_ => true);
    }


    private static async IAsyncEnumerable<ValueTuple<LoadoutFile.ReadOnly, Outcome<LspkPackageFormat.PakMetaData>>> GetAllPakMetadata(
        LoadoutFile.ReadOnly[] pakLoadoutFiles,
        IResourceLoader<Hash, Outcome<LspkPackageFormat.PakMetaData>> metadataPipeline,
        ILogger logger,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var pakLoadoutFile in pakLoadoutFiles)
        {
            Resource<Outcome<LspkPackageFormat.PakMetaData>> resource;
            try
            {
                resource = await metadataPipeline.LoadResourceAsync(pakLoadoutFile.Hash, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception while getting metadata for `{Name}`", pakLoadoutFile.AsLoadoutItemWithTargetPath().TargetPath.Item3);
                continue;
            }

            // Log the InvalidDataException case, but still return the resource
            if (resource.Data.Exception is not null)
            {
                logger.LogWarning(resource.Data.Exception, "Detected invalid BG3 Pak file: `{Name}`", pakLoadoutFile.AsLoadoutItemWithTargetPath().TargetPath.Item3);
            }

            yield return (pakLoadoutFile, resource.Data);
        }
    }

    private static LoadoutFile.ReadOnly[] GetAllPakLoadoutFiles(
        Loadout.ReadOnly loadout,
        bool onlyEnabledMods)
    {
        return loadout.Items
            .OfTypeLoadoutItemGroup()
            .Where(group => !onlyEnabledMods || !group.AsLoadoutItem().IsDisabled)
            // TODO: Quite inefficient way to find pak files, need to add markers on pak LoadoutFiles and mods containing them
            .SelectMany(group => group.Children.OfTypeLoadoutItemWithTargetPath()
                .OfTypeLoadoutFile()
                .Where(file => file.AsLoadoutItemWithTargetPath().TargetPath.Item2 == Bg3Constants.ModsLocationId &&
                               file.AsLoadoutItemWithTargetPath().TargetPath.Item3.Extension == Bg3Constants.PakFileExtension
                )
            )
            .ToArray();
    }

    private static readonly NamedLink NexusModsLink = new("Nexus Mods - Baldur's Gate 3", NexusModsUrlBuilder.GetGameUri(BaldursGate3.StaticDomain, campaign: NexusModsUrlBuilder.CampaignDiagnostics));
    private static readonly NamedLink BG3SENexusModsLink = new("Nexus Mods", NexusModsUrlBuilder.GetModUri(BaldursGate3.StaticDomain, ModId.From(2172), campaign: NexusModsUrlBuilder.CampaignDiagnostics));

#endregion Helpers
}
