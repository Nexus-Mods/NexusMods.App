using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Migrations.Migrations;

namespace NexusMods.DataModel.Migrations;

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
