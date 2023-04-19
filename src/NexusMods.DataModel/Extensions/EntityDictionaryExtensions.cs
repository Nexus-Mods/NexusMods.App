using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;

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
    /// <returns></returns>
    public static EntityDictionary<ModFileId, AModFile> ToEntityDictionary(
        [InstantHandle(RequireAwait = false)] this IEnumerable<AModFile> modFiles,
        IDataStore dataStore)
    {
        return new EntityDictionary<ModFileId, AModFile>(
            dataStore,
            modFiles
                .WithPersist(dataStore)
                .Select(modFile => new KeyValuePair<ModFileId, IId>(
                    modFile.Id,
                    modFile.DataStoreId)));
    }
}
