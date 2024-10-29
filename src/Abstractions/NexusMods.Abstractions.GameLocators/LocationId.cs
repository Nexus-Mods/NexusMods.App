using System.Collections.Immutable;
using TransparentValueObjects;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// The base folder for the GamePath, more values can easily be added here as needed
/// </summary>
[ValueObject<ushort>]
public readonly partial struct LocationId
{
    private static ImmutableDictionary<ushort, string> _cache = ImmutableDictionary<ushort, string>.Empty;
    
    /// <summary>
    /// Converts the string to a LocationId, if the string is not already interned it will be added to the cache, if a hash collision is
    /// detected (highly unlikely) an exception will be thrown.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static LocationId From(string value)
    {
        var hash = FNV1aHash.Hash(value);
        var mixed = (ushort) ((hash >> 16) ^ (hash & 0xFFFF));
        if (_cache.TryGetValue(mixed, out var existing))
        {
            if (existing != value)
            {
                throw new InvalidOperationException($"Hash collision detected for '{value}' and '{existing}'");
            }
            return From(mixed);
        }
        
        while (true)
        {
            var newCache = _cache.Add(mixed, value);
            if (ReferenceEquals(Interlocked.CompareExchange(ref _cache, newCache, _cache), _cache))
                break;
        }
        return From(mixed);
    }

    /// <inheritdoc />
    public override string ToString() => _cache[Value];
    
    /// <summary>
    /// Unknown game folder type, used for default values.
    /// </summary>
    public static readonly LocationId Unknown = From("Unknown");

    /// <summary>
    /// The path for the game installation.
    /// </summary>
    public static readonly LocationId Game = From("Game");

    /// <summary>
    /// Path used to store the save data of a game.
    /// </summary>
    public static readonly LocationId Saves = From("Saves");

    /// <summary>
    /// Path used to store player settings/preferences.
    /// </summary>
    public static readonly LocationId Preferences = From("Preferences");

    /// <summary>
    /// Path for game files located under LocalAppdata or equivalent.
    /// </summary>
    public static readonly LocationId AppData = From("AppData");

    /// <summary>
    /// Path for game files located under Appdata/Roaming or equivalent.
    /// </summary>
    public static readonly LocationId AppDataRoaming = From("AppDataRoaming");
}
