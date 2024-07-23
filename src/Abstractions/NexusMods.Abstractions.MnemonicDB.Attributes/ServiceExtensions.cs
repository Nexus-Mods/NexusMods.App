using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Extension methods.
/// </summary>
[PublicAPI]
public static class ServiceExtensions
{
    /// <summary>
    /// Wrapper around AddAttributeCollection.
    /// </summary>
    public static IServiceCollection AddModelDefinition<TModel>(this IServiceCollection serviceCollection)
        where TModel : class, IModelDefinition
    {
        return serviceCollection.AddAttributeCollection(typeof(TModel));
    }
}
