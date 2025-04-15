using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
namespace NexusMods.Abstractions.GC.DataModel;

/// <summary>
///     Adds GC DataModel related serialization services.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds GC DataModel related serialization services.
    /// </summary>
    public static IServiceCollection AddGcDataModel(this IServiceCollection services)
    {
        return services
            .AddAttributeCollection(typeof(BackedUpFile));
    }
}
