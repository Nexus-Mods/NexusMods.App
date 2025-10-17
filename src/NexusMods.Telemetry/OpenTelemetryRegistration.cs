using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Sdk.Tracking;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace NexusMods.Telemetry;

public static class OpenTelemetryRegistration
{
    public static IServiceCollection AddTelemetry(
        this IServiceCollection serviceCollection, 
        TrackingSettings? settings)
    {
        // NOTE(erri120): see this for debugging:
        // https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/README.md#self-diagnostics

        // OpenTelemetry gets added to DI and can't be disabled at runtime.
        // If OpenTelemetry isn't added to the DI container, none of the listeners for
        // Activities and Meters will be added as well.
        if (settings is null || !settings.EnableTracking) return serviceCollection;

        var openTelemetryBuilder = serviceCollection.AddOpenTelemetry();

        // A "resource" is an entity for which telemetry is recorded.
        // https://opentelemetry.io/docs/concepts/glossary/#resource
        // In a cloud native environment, this would contain information
        // about the docker container, the cluster, the cloud provider,
        // and other details about the environment.
        // We don't care about any of this, we just have to set some basic
        // attributes.
        openTelemetryBuilder.ConfigureResource(builder =>
        {
            // remove all build-in resources added by the SDK
            builder.Clear();

            // The SDK usually works with "detectors" to gather information about
            // the current environment. We don't care about that, we just have
            // one static resource.
            var resource = new Resource(Constants.CreateAttributes());
            builder.AddDetector(new WrappingResourceDetector(resource));
        });

        openTelemetryBuilder.WithMetrics(builder =>
        {
            builder.AddOtlpExporter((exporterOptions, metricReaderOptions) =>
            {
                metricReaderOptions.PeriodicExportingMetricReaderOptions = new PeriodicExportingMetricReaderOptions
                {
                    // This option allows us to change the interval at which observable instruments are
                    // getting observed. The SDK will observe the registered instruments every X milliseconds
                    // and send the metrics to the exporter.

                    // NOTE(erri120): don't change this
                    ExportIntervalMilliseconds = (int)Constants.ExportInterval.TotalMilliseconds,
                };

                ConfigureOtlp(exporterOptions, isMetrics: true);
            });

            // Using the deferred provider we can register Meters after the IServiceProvider has been created
            if (builder is not IDeferredMeterProviderBuilder deferredBuilder) throw new NotSupportedException();
            deferredBuilder.Configure(ConfigureMeterProviderBuilder);
        });

        return serviceCollection;
    }

    private static void ConfigureOtlp(OtlpExporterOptions exporterOptions, bool isMetrics)
    {
        exporterOptions.TimeoutMilliseconds = (int)Constants.ExporterTimeout.TotalMilliseconds;
        exporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;

        exporterOptions.Endpoint = isMetrics ? Constants.MetricsEndpoint : Constants.TracesEndpoint;
    }

    private static void ConfigureMeterProviderBuilder(
        IServiceProvider serviceProvider,
        MeterProviderBuilder meterProviderBuilder)
    {
        // NOTE(erri120): this approach prevents anyone from registering new Meters
        // and forces all consumers to use the same Meter. We don't need more than
        // one Meter for our purposes and additional Meters will just complicate
        // making sense of the data.
        var meter = Constants.Meter;
        var meterConfig = new MeterConfig(meter);

        // The SDK requires that we know all names of all Meters we want to use up front.
        meterProviderBuilder.AddMeter(meter.Name);

        var telemetryProviders = serviceProvider.GetServices<ITelemetryProvider>().ToArray();
        foreach (var telemetryProvider in telemetryProviders)
        {
            // deferred configuration of metrics until the DI container is available
            telemetryProvider.ConfigureMetrics(meterConfig);
        }
    }
}
