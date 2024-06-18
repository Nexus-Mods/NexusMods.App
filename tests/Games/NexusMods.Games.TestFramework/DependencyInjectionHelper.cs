using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;
using NexusMods.Activities;
using NexusMods.CrossPlatform;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.Settings;
using NexusMods.StandardGameLocators;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Games.TestFramework;

/// <summary>
/// Helper functions for dealing with dependency injection.
/// </summary>
[PublicAPI]
public static class DependencyInjectionHelper
{
    /// <summary>
    /// Adds the following default services to the provided <see cref="IServiceCollection"/> for testing:
    /// <list type="bullet">
    ///     <item>Logging via <see cref="LoggingServiceCollectionExtensions.AddLogging(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/></item>
    ///     <item><see cref="IFileSystem"/> via <see cref="Paths.Services.AddFileSystem"/></item>
    ///     <item><see cref="TemporaryFileManager"/> singleton</item>
    ///     <item><see cref="HttpClient"/> singleton</item>
    ///     <item>Nexus Web API via <see cref="Networking.NexusWebApi.Services.AddNexusWebApi"/></item>
    ///     <item><see cref="IHttpDownloader"/> via <see cref="Networking.HttpDownloader.Services.AddHttpDownloader"/></item>
    ///     <item>All services related to the <see cref="NexusMods.DataModel"/> via <see cref="DataModel.Services.AddDataModel"/></item>
    ///     <item>File extraction services via <see cref="NexusMods.FileExtractor.Services.AddFileExtractors"/></item>
    /// </list>
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <returns></returns>
    public static IServiceCollection AddDefaultServicesForTesting(this IServiceCollection serviceCollection)
    {
        var prefix = FileSystem.Shared
            .GetKnownPath(KnownPath.EntryDirectory)
            .Combine($"NexusMods.Games.TestFramework-{Guid.NewGuid()}");

        return serviceCollection
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
            .AddSingleton<JsonConverter, GameInstallationConverter>()
            .AddFileSystem()
            .AddSingleton<TemporaryFileManager>(_ => new TemporaryFileManager(FileSystem.Shared, prefix))
            .AddSingleton<HttpClient>()
            .AddSingleton<TestModDownloader>()
            .AddNexusWebApi(true)
            .AddCrossPlatform()
            .AddActivityMonitor()
            .AddSettings<LoggingSettings>()
            .AddHttpDownloader()
            .AddDataModel()
            .AddLoadoutsSynchronizers()
            .OverrideSettingsForTests<DataModelSettings>(settings => settings with
            {
                UseInMemoryDataModel = true,
            })
            .AddSettingsManager()
            .AddFileExtractors();
    }

    /// <summary>
    /// Finds an implementation <typeparamref name="TImplementation"/> of
    /// <typeparamref name="TInterface"/> inside the provided DI container.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <typeparam name="TImplementation"></typeparam>
    /// <typeparam name="TInterface"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception">Thrown when the implementation hasn't been registered in the DI container.</exception>
    public static TImplementation FindImplementationInContainer<TImplementation, TInterface>(this IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetService(typeof(TImplementation));
        if (service is TImplementation implementation) return implementation;

        var implementations = serviceProvider.GetServices(typeof(TInterface));
        if (implementations is null)
            throw new Exception($"{typeof(TImplementation)} is not registered in the DI container!");

        var validImplementations = implementations.OfType<TImplementation>();
        var validImplementation = validImplementations.FirstOrDefault();

        if (validImplementation is null)
            throw new Exception($"{typeof(TImplementation)} is not registered in the DI container!");

        return validImplementation;
    }
}
