using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.HighPerformance.Buffers;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Sdk;
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
        private readonly EventDefinition _eventDefinition;
        private readonly HashSet<string>? _writtenProperties;

        private EventWriter(EventTracker tracker, Utf8JsonWriter jsonWriter, EventDefinition eventDefinition)
        {
            _tracker = tracker;
            _jsonWriter = jsonWriter;
            _eventDefinition = eventDefinition;
            _writtenProperties = ApplicationConstants.IsDebug ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) : null;
        }

        public void Write<T>(JsonEncodedText propertyName, T propertyValue)
        {
            if (!_tracker.ValidateProperty(propertyName.Value, propertyValue)) return;
            _jsonWriter.WritePropertyName(propertyName);
            JsonSerializer.Serialize(_jsonWriter, propertyValue, _tracker._jsonSerializerOptions);
        }

        public void Write<T>(string propertyName, T propertyValue)
        {
            if (!_tracker.ValidateProperty(propertyName, propertyValue)) return;
            _jsonWriter.WritePropertyName(propertyName);
            JsonSerializer.Serialize(_jsonWriter, propertyValue, _tracker._jsonSerializerOptions);
        }

        public void Write<T>((string name, T value) property)
        {
            ValidatePropertyDefinition<T>(property.name);
            Write(property.name, property.value);
        }
        
        [Conditional("DEBUG")]
        private void ValidatePropertyDefinition<T>(string name)
        {
            Debug.Assert(_writtenProperties is not null);
            if (_writtenProperties.Contains(name))
            {
                throw new InvalidOperationException($"Property `{name}` has already been added to the event `{_eventDefinition.Name.Value}`");
            }

            if (!_eventDefinition.TryGet<T>(name, out var propertyDefinition))
            {
                throw new InvalidOperationException($"Event definition `{_eventDefinition.Name.Value}` doesn't contain a property definition for `{name}`");
            }

            if (propertyDefinition.Type != typeof(T))
            {
                throw new InvalidOperationException($"Property definition type mismatch for property `{name}` on event `{_eventDefinition.Name.Value}`: expected `{propertyDefinition.Type}` but received `{typeof(T)}`");
            }

            _writtenProperties.Add(name);
        }

        [Conditional("DEBUG")]
        private void ValidateAllPropertyDefinitions()
        {
            Debug.Assert(_writtenProperties is not null);
            foreach (var propertyDefinition in _eventDefinition)
            {
                if (propertyDefinition.IsOptional) continue;
                if (_writtenProperties.TryGetValue(propertyDefinition.Name.Value, out _)) continue;
                throw new InvalidOperationException($"Missing required property `{propertyDefinition.Name.Value}` on event `{_eventDefinition.Name.Value}`");
            }
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
                const int insertIdLimit = 36;

                var insertId = Guid.CreateVersion7(now);
                Write(JsonText.InsertId, insertId);
            }

            _jsonWriter.WriteString(JsonText.OS, _tracker._osInformation.MatchPlatform(
                onLinux: () => JsonText.Linux,
                onWindows: () => JsonText.Windows,
                onOSX: () => JsonText.OSX
            ));
        }

        public static EventWriter Create(EventTracker tracker, ArrayPoolBufferWriter<byte> bufferWriter, EventDefinition eventDefinition)
        {
            var jsonWriter = tracker.GetWriter(bufferWriter);

            jsonWriter.WriteStartObject();
            jsonWriter.WriteString(JsonText.Event, eventDefinition.Name);
            jsonWriter.WriteStartObject(JsonText.Properties);

            var writer = new EventWriter(tracker, jsonWriter, eventDefinition);
            writer.WriteSuperProperties();
            return writer;
        }

        public void Dispose()
        {
            ValidateAllPropertyDefinitions();

            _jsonWriter.WriteEndObject(); // properties
            _jsonWriter.WriteEndObject(); // event

            _tracker.ReturnWriter(_jsonWriter);
        }
    }
}
