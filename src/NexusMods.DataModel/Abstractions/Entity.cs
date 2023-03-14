using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Exceptions;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Base class used for an item stored within Nexus App's database.
/// Example entities include: <br/>
/// - <see cref="Mod"/><br/>
/// - <see cref="Loadout"/><br/>
/// - <see cref="AnalyzedFile"/><br/>
/// - <see cref="AnalyzedArchive"/>
/// </summary>
public abstract record Entity : IWalkable<Entity>
{
    /*
        There's a part of implementation detail here that might be unclear to people
        not familiar with C# in depth.

        How do we keep track of IDs and Changes?

        Because we use records (immutable), a new copy of the object is
        created every time you modify the object by using the `with` keyword, or similar.

        To do the copy, the language calls the copy constructor (see below); this
        copy constructor copies everything *except* for the ID; which means that
        if you modify the object in any way; the ID is missing and thus the object
        is considered not persistent in the database.

        Let me know if this was useful to you

        - Sewer

        i.e. If you ever add any fields here, please add them to the copy constructor!!
    */

    /// <summary>
    /// Marks the type of entity this is.
    ///
    /// This is used mostly for internal use and is used in actions
    /// such as determining which table to use in data store (database).
    /// </summary>
    [JsonIgnore]
    public abstract EntityCategory Category { get; }

    private IId? _id;

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Entity(Entity self)
    {
    }

    /// <summary/>
    protected Entity() { }

    /// <summary>
    /// Writes the current value to the database.
    /// </summary>
    protected virtual IId Persist(IDataStore store)
    {
        return store.Put(this);
    }

    /// <summary>
    /// Ensures this item is stored in the database.
    /// </summary>
    public void EnsurePersisted(IDataStore store)
    {
        _id ??= Persist(store);
    }

    /// <summary>
    /// ID of this item from within the data store.
    /// If this item is not in the store, it is persisted.
    /// </summary>
    [JsonIgnore]
    public IId DataStoreId
    {
        get => _id ?? ThrowUnpersistedEntity();
        set => _id = value;
    }

    /// <summary>
    /// Returns true if this item is persisted in the data store and has an ID.
    /// </summary>
    [JsonIgnore]
    public bool IsPersisted => _id != null;

    /// <inheritdoc />
    public TState Walk<TState>(Func<TState, Entity, TState> visitor, TState initial)
    {
        // TODO: cache this as a Linq.Expression compiled lambda, for now just use reflection
        var state = visitor(initial, this);
        foreach (var property in GetType().GetProperties())
        {
            if (!property.PropertyType.IsAssignableTo(typeof(IWalkable<Entity>)))
                continue;

            var value = property.GetValue(this);
            if (value is IWalkable<Entity> walkable)
            {
                state = walkable.Walk(visitor, state);
            }
        }

        return state;
    }

    // Throwing prevents inlining which is costly in copy constructor
    // thus I moved the throw into a separate method so the constructor
    // can be inlined for faster mutations. - Sewer
    private static IId ThrowUnpersistedEntity() => throw new UnpersistedEntity();
}
