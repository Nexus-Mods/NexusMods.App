namespace NexusMods.Abstractions.Telemetry;

internal static class Helpers
{
    public static KeyValuePair<string, object?> ToTag(this string gameName)
    {
        return new KeyValuePair<string, object?>(InstrumentConstants.TagGame, gameName);
    }
}
