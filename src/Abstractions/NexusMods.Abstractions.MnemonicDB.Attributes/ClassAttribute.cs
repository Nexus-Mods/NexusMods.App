using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// A attribute that will resolve down to a specific class instance, that is resolved via the DI container.
/// </summary>
/// <param name="ns"></param>
/// <param name="name"></param>
public class ClassAttribute<TType>(string ns, string name) : ScalarAttribute<UInt128, UInt128>(ValueTags.UInt128, ns, name)
where TType : IGuidClass
{
    /// <inheritdoc />
    protected override UInt128 ToLowLevel(UInt128 id) => id;

    /// <inheritdoc />
    protected override UInt128 FromLowLevel(UInt128 value, ValueTags tags) => value;
    
    /// <summary>
    /// Gets the instance of the class from the DI container.
    /// </summary>
    public TType GetInstance(Entity entity) 
    {
        var key = Get(entity);
        var item = entity.Db.Connection.ServiceProvider.GetKeyedService<TType>(key);
        if (item == null)
            throw new KeyNotFoundException($"The item {typeof(TType)} with key {key} was not found in the DI container.");
        return item;
    }
}

/// <summary>
/// A class that has a unique identifier.
/// </summary>
public interface IGuidClass
{
    /// <summary>
    /// Gets the same unique identifier for the class
    /// </summary>
    public static abstract UInt128 Guid { get; }
}
