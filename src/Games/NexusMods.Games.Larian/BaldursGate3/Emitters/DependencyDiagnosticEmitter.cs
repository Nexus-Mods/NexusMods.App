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

namespace NexusMods.Games.Larian.BaldursGate3.Emitters;

public class DependencyDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger _logger;
    private readonly IResourceLoader<Hash, LsxXmlFormat.MetaFileData> _metadataPipeline;

    public DependencyDiagnosticEmitter(IServiceProvider serviceProvider, ILogger<DependencyDiagnosticEmitter> logger)
    {
        _logger = logger;
        _metadataPipeline = Pipelines.GetMetadataPipeline(serviceProvider);
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var diagnostics = await DiagnoseDependenciesAsync(loadout, cancellationToken);
        foreach (var diagnostic in diagnostics)
        {
            yield return diagnostic;
        }
    }

#region Diagnosers

    private async Task<IEnumerable<Diagnostic>> DiagnoseDependenciesAsync(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var pakLoadoutFiles = GetAllPakLoadoutFiles(loadout, onlyEnabledMods: true);
        var allFileMetadata = await GetAllPakMetadata(pakLoadoutFiles,
                _metadataPipeline,
                _logger,
                cancellationToken
            )
            .ToArrayAsync(cancellationToken: cancellationToken);

        var diagnostics = new List<Diagnostic>();

        foreach (var metaFileData in allFileMetadata)
        {
            var dependencies = metaFileData.Item2.Dependencies;
            var loadoutItemGroup = metaFileData.Item1.AsLoadoutItemWithTargetPath().AsLoadoutItem().Parent;

            foreach (var dependency in dependencies)
            {
                var dependencyUuid = dependency.Uuid;

                if (dependencyUuid == string.Empty || allFileMetadata.Any(x => x.Item2.ModuleShortDesc.Uuid == dependencyUuid))
                {
                    continue;
                }

                // add diagnostic
                diagnostics.Add(Diagnostics.CreateMissingDependency(
                        ModuleName: loadoutItemGroup.ToReference(loadout),
                        MissingDependencyName: dependency.Name,
                        PakModuleName: metaFileData.Item2.ModuleShortDesc.Name,
                        NexusModsLink: NexusModsLink
                    )
                );
            }
        }

        return diagnostics;
    }

#endregion Diagnosers

#region Helpers

    private static async IAsyncEnumerable<ValueTuple<LoadoutFile.ReadOnly, LsxXmlFormat.MetaFileData>> GetAllPakMetadata(
        LoadoutFile.ReadOnly[] pakLoadoutFiles,
        IResourceLoader<Hash, LsxXmlFormat.MetaFileData> metadataPipeline,
        ILogger logger,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var pakLoadoutFile in pakLoadoutFiles)
        {
            Resource<LsxXmlFormat.MetaFileData> resource;
            try
            {
                resource = await metadataPipeline.LoadResourceAsync(pakLoadoutFile.Hash, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception while getting metadata for `{Name}`", pakLoadoutFile.AsLoadoutItemWithTargetPath().TargetPath.Item3);
                continue;
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

    private static readonly NamedLink NexusModsLink = new("Nexus Mods", NexusModsUrlBuilder.CreateGenericUri("https://nexusmods.com/baldursgate3"));

#endregion Helpers
}
