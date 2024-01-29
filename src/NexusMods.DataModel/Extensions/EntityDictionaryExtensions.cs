using DynamicData;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.DataModel.Extensions;

/// <summary>
/// Extension methods for <see cref="EntityDictionary{TK,TV}"/>
/// </summary>
[PublicAPI]
public static class EntityDictionaryExtensions
{
    /// <summary>
    /// Creates a new <see cref="EntityDictionary{TK,TV}"/> from an enumerable
    /// of a collection of items that inherit from <see cref="AModFile"/>.
    /// </summary>
    /// <param name="modFiles"></param>
    /// <param name="dataStore"></param>
    /// <param name="persist">Whether to use persist the collection of entities in the data store.</param>
    /// <returns></returns>
    public static EntityDictionary<ModFileId, AModFile> ToEntityDictionary(
        [InstantHandle(RequireAwait = false)] this IEnumerable<AModFile> modFiles,
        IDataStore dataStore,
        bool persist = true)
    {
        var enumerable = persist
            ? modFiles.WithPersist(dataStore)
            : modFiles;

        return new EntityDictionary<ModFileId, AModFile>(
            dataStore,
            enumerable
                .Select(modFile => new KeyValuePair<ModFileId, IId>(
                    modFile.Id,
                    modFile.DataStoreId)));
    }
}
