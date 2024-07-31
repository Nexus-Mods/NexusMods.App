using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Attribute for resolving and returning a <see cref="IPersistedJobWorker"/> instance from the database.
/// </summary>
/// <param name="ns"></param>
/// <param name="name"></param>
public class WorkerAttribute(string ns, string name) : ScalarAttribute<UInt128, UInt128>(ValueTags.Int128, ns, name)
{
    /// <inheritdoc />
    protected override UInt128 ToLowLevel(UInt128 value)
    {
        return value;
    }

    protected override UInt128 FromLowLevel(UInt128 value, ValueTags tags)
    {
        return value;
    }
    
    /// <summary>
    /// Get the worker instance from the DI container that has a unique identifier matching the value stored in this attribute
    /// </summary>
    public IPersistedJobWorker GetWorker<TModel>(in TModel entity)
    where TModel : IReadOnlyModel<TModel>
    {
        var key = Get(entity);
        var item = entity.Db.Connection.ServiceProvider.GetKeyedService<IPersistedJobWorker>(key);
        if (item == null)
            throw new KeyNotFoundException($"The item {typeof(IPersistedJobWorker)} with key {key} was not found in the DI container.");
        return item;
    }
}
