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
using NexusMods.Hashing.xxHash64;
using OneOf.Types;

namespace NexusMods.Games.Larian.BaldursGate3.Emitters;

public class DependencyDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger _logger;
    private readonly IResourceLoader<Hash, OneOf.OneOf<LsxXmlFormat.MetaFileData, Error<InvalidDataException>>> _metadataPipeline;

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
            if (metadataOrError.IsT1)
            {
                diagnostics.Add(Diagnostics.CreateInvalidPakFile(
                    ModName: loadoutItemGroup.ToReference(loadout),
                    PakFileName: mod.AsLoadoutItemWithTargetPath().TargetPath.Item3.FileName
                    ));
                continue;
            }
            
            // non error case
            var metadata = metadataOrError.AsT0;
            var dependencies = metadata.Dependencies;
            

            foreach (var dependency in dependencies)
            {
                var dependencyUuid = dependency.Uuid;

                if (dependencyUuid == string.Empty || metaFileTuples.Any(x => x.Item2.IsT0 && x.Item2.AsT0.ModuleShortDesc.Uuid == dependencyUuid))
                {
                    continue;
                }

                // add diagnostic
                diagnostics.Add(Diagnostics.CreateMissingDependency(
                        ModName: loadoutItemGroup.ToReference(loadout),
                        MissingDepName: dependency.Name,
                        MissingDepVersion: dependency.SemanticVersion.ToString(),
                        PakModuleName: metadata.ModuleShortDesc.Name,
                        PakModuleVersion: metadata.ModuleShortDesc.SemanticVersion.ToString(),
                        NexusModsLink: NexusModsLink
                    )
                );
            }
        }

        return diagnostics;
    }

#endregion Diagnosers

#region Helpers

    private static async IAsyncEnumerable<ValueTuple<LoadoutFile.ReadOnly, OneOf.OneOf<LsxXmlFormat.MetaFileData, Error<InvalidDataException>>>> GetAllPakMetadata(
        LoadoutFile.ReadOnly[] pakLoadoutFiles,
        IResourceLoader<Hash, OneOf.OneOf<LsxXmlFormat.MetaFileData, Error<InvalidDataException>>> metadataPipeline,
        ILogger logger,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var pakLoadoutFile in pakLoadoutFiles)
        {
            Resource<OneOf.OneOf<LsxXmlFormat.MetaFileData, Error<InvalidDataException>>> resource;
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
            if (resource.Data.IsT1)
            {
                logger.LogWarning(resource.Data.AsT1.Value, "Detected invalid BG3 Pak file: `{Name}`", pakLoadoutFile.AsLoadoutItemWithTargetPath().TargetPath.Item3);
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
