using OneOf;

namespace NexusMods.Telemetry;

internal record struct EventData(EventDefinition Definition, EventMetadata Metadata);

internal record struct TrackingData(OneOf<EventData, ExceptionData> Data);
