using Microsoft.Extensions.Logging;

namespace NexusMods.Backend;

internal static partial class Logging
{
    [LoggerMessage(
        level: LogLevel.Debug,
        message: "Event property `{propertyName}` validation failed because the string has a length of `{StringLength}` which is above the allowed limit of `{Limit}`. Value is `{Value}`"
    )]
    public static partial void EventStringValueValidationFailed(ILogger logger, string propertyName, int stringLength, int limit, string value);

    [LoggerMessage(
        level: LogLevel.Debug,
        message: "Event property `{PropertyName}` validation failed because the collection has `{Count}` elements which is above the allowed limit of `{Limit}` elements. Value is of type `{Type}`"
    )]
    public static partial void EventCollectionValueValidationFailed(ILogger logger, string propertyName, int count, int limit, Type type);
}
