using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.Games;

/// <summary>
///     Adds games related serialization services.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Adds known Game entity related serialization services.
    /// </summary>
    public static IServiceCollection AddGames(this IServiceCollection services)
    {
        return services
            .AddSortOrderItemModel()
            .AddSortOrderQueriesSql();
    }
}
