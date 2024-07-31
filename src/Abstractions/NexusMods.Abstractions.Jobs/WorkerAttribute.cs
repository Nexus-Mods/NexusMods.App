using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Attribute for resolving and returning a <see cref="IPersistedJobWorker"/> instance from the database.
/// </summary>
/// <param name="ns"></param>
/// <param name="name"></param>
public class WorkerAttribute(string ns, string name) : ScalarAttribute<IPersistedJobWorker, UInt128>(ValueTags.UInt128, ns, name)
{
    /// <inheritdoc />
    protected override UInt128 ToLowLevel(IPersistedJobWorker worker)
    {
        Span<byte> bytes = stackalloc byte[16];
        MemoryMarshal.TryWrite(bytes, worker.Id);
        return MemoryMarshal.Read<UInt128>(bytes);
    }

    /// <inheritdoc />
    protected override IPersistedJobWorker FromLowLevel(UInt128 value, ValueTags tags, RegistryId registryId)
    {
        var provider = GetServiceProvider(registryId).GetServices<IPersistedJobWorker>();
        Span<byte> bytes = stackalloc byte[16];
        MemoryMarshal.Write(bytes, value);
        var guid = new Guid(bytes);
        var result = provider.FirstOrDefault(worker => worker.Id == guid);
        
        if (result is null)
            throw new InvalidOperationException($"Could not find a worker with the ID {guid}.");
        
        return result;
    }
}
