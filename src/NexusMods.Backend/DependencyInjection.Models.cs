using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers.Conflicts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.DataModel.SchemaVersions;
using NexusMods.Networking.GOG.Models;
using NexusMods.Networking.NexusWebApi.Auth;
using NexusMods.Networking.NexusWebApi.UpdateFilters;
using NexusMods.Networking.NexusWebApi.V1Interop;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Library;
using NexusMods.Sdk.Loadouts;
using NexusMods.Sdk.Resources;

namespace NexusMods.Backend;

public static partial class DependencyInjection
{
    /// <summary>
    /// Registers all Mnemonic database models
    /// </summary>
    public static IServiceCollection AddDatabaseModels(this IServiceCollection serviceCollection)
    {
        // NOTE(erri120): To make merge conflicts easier to handle, please sort this list lexicographically
        return serviceCollection
            .AddApiKeyModel()
            .AddAuthInfoModel()
            .AddCollectionCategoryModel()
            .AddCollectionDownloadBundledModel()
            .AddCollectionDownloadExternalModel()
            .AddCollectionDownloadModel()
            .AddCollectionDownloadNexusModsModel()
            .AddCollectionDownloadRulesModel()
            .AddCollectionGroupModel()
            .AddCollectionMetadataModel()
            .AddCollectionRevisionMetadataModel()
            .AddDeletedFileModel()
            .AddDirectDownloadLibraryFileModel()
            .AddDiskStateEntryModel()
            .AddDownloadedFileModel()
            .AddEpicGameStoreBuildModel()
            .AddGameBackedUpFileModel()
            .AddGameDomainToGameIdMappingModel()
            .AddGameInstallMetadataModel()
            .AddGogBuildModel()
            .AddGogDepotModel()
            .AddGogManifestModel()
            .AddHashRelationModel()
            .AddIgnoreFileUpdateModel()
            .AddJWTTokenModel()
            .AddLibraryArchiveFileEntryModel()
            .AddLibraryArchiveModel()
            .AddLibraryFileModel()
            .AddLibraryItemModel()
            .AddLibraryLinkedLoadoutItemModel()
            .AddLoadoutFileModel()
            .AddLoadoutGameFilesGroupModel()
            .AddLoadoutItemGroupModel()
            .AddLoadoutItemGroupPriorityModel()
            .AddLoadoutItemModel()
            .AddLoadoutItemWithTargetPathModel()
            .AddLoadoutModel()
            .AddLoadoutOverridesGroupModel()
            .AddLoadoutSnapshotModel()
            .AddLocalFileModel()
            .AddManagedCollectionLoadoutGroupModel()
            .AddManuallyAddedGameModel()
            .AddManuallyCreatedArchiveModel()
            .AddMigrationLogItemModel()
            .AddNexusCollectionBundledLoadoutGroupModel()
            .AddNexusCollectionItemLoadoutGroupModel()
            .AddNexusCollectionLoadoutGroupModel()
            .AddNexusCollectionReplicatedLoadoutGroupModel()
            .AddNexusModsCollectionLibraryFileModel()
            .AddNexusModsFileMetadataModel()
            .AddNexusModsLibraryItemModel()
            .AddNexusModsModPageMetadataModel()
            .AddPathHashRelationModel()
            .AddPersistedDbResourceModel()
            .AddSchemaVersionModel()
            .AddSettingModel()
            .AddSortOrderItemModel()
            .AddSortOrderModel()
            .AddSteamManifestModel()
            .AddUserModel()
            .AddVersionDefinitionModel()
; // NOTE(erri120): kept on a separate line for easier merge conflicts
    }
}
