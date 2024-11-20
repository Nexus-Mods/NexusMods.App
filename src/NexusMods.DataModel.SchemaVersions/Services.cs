using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.SchemaVersions.Migrations;

namespace NexusMods.DataModel.SchemaVersions;

public static class Services
{
    public static IServiceCollection AddMigrations(this IServiceCollection services)
    {
        services.AddSchemaVersionModel();
        services.AddSingleton<IMigration, UpsertFingerprint>();
        services.AddSingleton<MigrationService>();
        return services;
    }
    
}
