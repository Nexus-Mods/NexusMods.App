using System.Collections;
using System.Text.Json;
using CommunityToolkit.HighPerformance.Buffers;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Sdk.Tracking;

namespace NexusMods.Backend.Tracking;

internal partial class EventTracker
{
    private bool ValidateProperty<T>(string name, T value)
    {
        return value switch
        {
            string text => ValidateString(name, text),
            ICollection collection => ValidateCollection(name, collection),
            _ => true,
        };
    }

    private bool ValidateString(string name, string text)
    {
        // https://developer.mixpanel.com/reference/import-events#common-issues
        // "We truncate all strings down to 255 characters."
        const int limit = 255;
        if (text.Length < limit) return true;

        Logging.EventStringValueValidationFailed(_logger, name, text.Length, limit, text);
        return false;
    }

    private bool ValidateCollection(string name, ICollection collection)
    {
        // https://developer.mixpanel.com/reference/import-events#high-level-requirements
        // "All array properties must have fewer than 255 elements."
        const int limit = 255;
        if (collection.Count < limit) return true;

        Logging.EventCollectionValueValidationFailed(_logger, name, collection.Count, limit, collection.GetType());
        return false;
    }

    private readonly struct EventWriter : IDisposable
    {
        private readonly EventTracker _tracker;
        private readonly Utf8JsonWriter _jsonWriter;

        private EventWriter(EventTracker tracker, Utf8JsonWriter jsonWriter)
        {
            _tracker = tracker;
            _jsonWriter = jsonWriter;
        }

        private void Write<T>(EventString propertyName, T? propertyValue)
        {
            if (propertyValue is null) return;
            if (!_tracker.ValidateProperty(propertyName.Value, propertyValue)) return;
            _jsonWriter.WritePropertyName(propertyName.EncodedText);
            JsonSerializer.Serialize(_jsonWriter, propertyValue, _tracker._jsonSerializerOptions);
        }

        public void Write<T>((EventString name, T? value) property)
        {
            Write(property.name, property.value);
        }

        private void WriteSuperProperties()
        {
            // https://developer.mixpanel.com/reference/event-deduplication#deduplication-example
            // The time an event occurred. If present, the value should be a unix timestamp (seconds since midnight, January 1st, 1970 - UTC).
            // If this property is not included in your request, Mixpanel will use the time the event arrives at the server.
            // https://developer.mixpanel.com/reference/import-events#propertiestime
            // The time at which the event occurred, in seconds or milliseconds since epoch.
            // We require a value for time. We will reject events with time values that are before 1971-01-01 or more than 1 hour in the future as measured on our servers.
            // If the time value is set in the future, it will be overwritten with the current present time at ingestion.
            var now = _tracker._timeProvider.GetUtcNow();
            _jsonWriter.WriteNumber(JsonText.Time, now.ToUnixTimeSeconds());

            _jsonWriter.WriteString(JsonText.AppName, JsonText.AppNameValue);
            _jsonWriter.WriteString(JsonText.AppVersion, JsonText.AppVersionValue);
            _jsonWriter.WriteString(JsonText.PlatformType, JsonText.PlatformTypeValue);
            _jsonWriter.WriteString(JsonText.Token, JsonText.ProjectToken);
            _jsonWriter.WriteString(JsonText.DeviceId, _tracker._deviceId);

            // https://docs.mixpanel.com/docs/tracking-methods/id-management#distinct-id
            // https://docs.mixpanel.com/docs/tracking-methods/id-management/identifying-users-simplified
            var user = _tracker._loginManager.UserInfo;
            if (user is not null)
            {
                Write(JsonText.UserId, user.UserId.Value);
                Write(JsonText.DistinctId, user.UserId.Value);
                _jsonWriter.WriteString(JsonText.UserType, user.UserRole switch
                {
                    UserRole.Free => JsonText.Registered,
                    UserRole.Supporter => JsonText.Supporter,
                    UserRole.Premium => JsonText.Premium,
                });
            }
            else
            {
                _jsonWriter.WriteString(JsonText.DistinctId, _tracker._deviceId);
                _jsonWriter.WriteString(JsonText.UserType, JsonText.Anonymous);
            }

            // insert_id
            {
                // https://developer.mixpanel.com/reference/import-events#propertiesinsert_id
                // "$insert_ids must be ≤ 36 bytes and contain only alphanumeric characters or "-""
                // const int insertIdLimit = 36;

                var insertId = Guid.CreateVersion7(now);
                Write(JsonText.InsertId, insertId);
            }

            _jsonWriter.WriteString(JsonText.OS, _tracker._osInformation.MatchPlatform(
                onLinux: () => JsonText.Linux,
                onWindows: () => JsonText.Windows,
                onOSX: () => JsonText.OSX
            ));
        }

        public static EventWriter Create(EventTracker tracker, ArrayPoolBufferWriter<byte> bufferWriter, EventString name)
        {
            var jsonWriter = tracker.GetWriter(bufferWriter);

            jsonWriter.WriteStartObject();
            jsonWriter.WriteString(JsonText.Event, name.EncodedText);
            jsonWriter.WriteStartObject(JsonText.Properties);

            var writer = new EventWriter(tracker, jsonWriter);
            writer.WriteSuperProperties();
            return writer;
        }

        public void Dispose()
        {
            _jsonWriter.WriteEndObject(); // properties
            _jsonWriter.WriteEndObject(); // event

            _tracker.ReturnWriter(_jsonWriter);
        }
    }
}
