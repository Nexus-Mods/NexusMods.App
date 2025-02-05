using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.SchemaVersions.Migrations;

namespace NexusMods.DataModel.SchemaVersions;

public static class Services
{
    public static IServiceCollection AddMigrations(this IServiceCollection services)
    {
        services.AddSchemaVersionModel();
        services.AddMigrationLogItemModel();
        services.AddSingleton<MigrationService>();


        // Migrations go here:
        services.AddMigration<_0001_ConvertTimestamps>();
        services.AddMigration<_0002_RemoveGameFiles>();
        
        return services;
    }

    /// <summary>
    /// Add a migration to the DI container
    /// </summary>
    public static IServiceCollection AddMigration<T>(this IServiceCollection services) where T : IMigration
    {
        return services.AddSingleton<MigrationDefinition>(_ => new MigrationDefinition(T.IdAndName.Id, T.IdAndName.Name, typeof(T)))
            // Transient so that migrations can store data locally
            .AddTransient(typeof(T));
    }
}
