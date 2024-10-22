using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Games.Larian.BaldursGate3.Utils.LsxXmlParsing;
using NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;
using NexusMods.Hashing.xxHash64;
using Polly;

namespace NexusMods.Games.Larian.BaldursGate3.Emitters;

public class DependencyDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger _logger;
    private readonly IResourceLoader<Hash, Outcome<LspkPackageFormat.PakMetaData>> _metadataPipeline;

    public DependencyDiagnosticEmitter(IServiceProvider serviceProvider, ILogger<DependencyDiagnosticEmitter> logger)
    {
        _logger = logger;
        _metadataPipeline = Pipelines.GetMetadataPipeline(serviceProvider);
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var diagnostics = await DiagnosePakModulesAsync(loadout, cancellationToken);
        foreach (var diagnostic in diagnostics)
        {
            yield return diagnostic;
        }
    }

#region Diagnosers

    private async Task<IEnumerable<Diagnostic>> DiagnosePakModulesAsync(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
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
            var metadata = metadataOrError.Result;
            var dependencies = metadata.MetaFileData.Dependencies;

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
                            PakModuleName: metadata.MetaFileData.ModuleShortDesc.Name,
                            PakModuleVersion: metadata.MetaFileData.ModuleShortDesc.SemanticVersion.ToString(),
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
                            PakModuleName: metadata.MetaFileData.ModuleShortDesc.Name,
                            PakModuleVersion: metadata.MetaFileData.ModuleShortDesc.SemanticVersion.ToString(),
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

    private static readonly NamedLink NexusModsLink = new("Nexus Mods - Baldur's Gate 3", NexusModsUrlBuilder.CreateGenericUri("https://nexusmods.com/baldursgate3"));

#endregion Helpers
}
