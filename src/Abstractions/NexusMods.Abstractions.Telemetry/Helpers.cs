using NexusMods.Abstractions.Games.DTO;

namespace NexusMods.Abstractions.Telemetry;

internal static class Helpers
{
    public static KeyValuePair<string, object?> ToTag(this GameDomain gameDomain)
    {
        return new KeyValuePair<string, object?>(InstrumentConstants.TagGame, gameDomain.Value.ToLowerInvariant());
    }
}
