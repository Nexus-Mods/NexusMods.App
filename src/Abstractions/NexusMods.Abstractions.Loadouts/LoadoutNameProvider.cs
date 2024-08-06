namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Static class for providing new Loadout names.
/// </summary>
public static class LoadoutNameProvider
{
    private static readonly string[] Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(c => c.ToString()).ToArray();
    private static readonly string[] AllCombinations = GenerateAllCombinations();
    
    private static string[] GenerateAllCombinations()
    {
        var combinations = new List<string>(Alphabet);
        combinations.AddRange(from first in Alphabet from second in Alphabet select first + second);
        return combinations.ToArray();
    }

    /// <summary>
    /// Returns a capital one or two letter short name for a loadout.
    /// It will cycle through the alphabet, starting with 'A' and continue to AA.
    /// If all two letter combinations are used, a duplicate name will be returned.
    /// Short names should not be assumed to be unique, they are only used for user disambiguation.
    /// </summary>
    public static string GetNewShortName(string[] existingLoadoutsShortNames)
    {
        var usedNames = new HashSet<string>(existingLoadoutsShortNames);
        foreach (var name in AllCombinations)
        {
            if (!usedNames.Contains(name))
            {
                return name;
            }
        }
        // If all combinations are used, use "ZZ".
        return "ZZ";
    }
}
