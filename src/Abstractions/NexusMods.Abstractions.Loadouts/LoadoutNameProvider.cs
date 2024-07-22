namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Static class for providing new Loadout names.
/// </summary>
public static class LoadoutNameProvider
{
    private static readonly string[] Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(c => c.ToString()).ToArray();

    /// <summary>
    /// Returns a capital one-letter short name for a loadout.
    /// It will cycle through the alphabet, starting with 'A'.
    /// If all letters are used,'Z' will be returned.
    /// Short names should not be assumed to be unique, they are only used for user disambiguation.
    /// </summary>
    public static string GetNewShortName(string[] existingLoadoutsShortNames)
    {
        var usedLetters = new HashSet<string>(existingLoadoutsShortNames);
        foreach (var letter in Alphabet)
        {
            if (!usedLetters.Contains(letter))
            {
                return letter;
            }
        }
        // If all letters are used, use "Z".
        return "Z";
    }
}
