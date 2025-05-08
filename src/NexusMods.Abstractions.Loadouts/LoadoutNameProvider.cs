namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Static class for providing new Loadout names.
/// </summary>
public static class LoadoutNameProvider
{
    /// <summary>
    /// Returns a capital one or two letter short name for a loadout.
    /// It will cycle through the alphabet, starting with 'A' and continue to AA.
    /// If all two letter combinations are used, a duplicate name will be returned.
    /// Short names should not be assumed to be unique, they are only used for user disambiguation.
    /// </summary>
    public static string GetNewShortName(ReadOnlySpan<string> existingLoadoutsShortNames)
    {
        for (var i = 'A'; i <= 'Z'; i++)
        {
            var found = false;
            foreach (var name in existingLoadoutsShortNames)
            {
                if (name.Length != 1) continue;
                if (name[0] != i) continue;

                found = true;
                break;
            }

            if (!found) return $"{i}";
        }

        for (var i = 'A'; i <= 'Z'; i++)
        {
            for (var j = 'A'; j <= 'Z'; j++)
            {
                var found = false;
                foreach (var name in existingLoadoutsShortNames)
                {
                    if (name.Length != 2) continue;
                    if (name[0] != i || name[1] != j) continue;

                    found = true;
                    break;
                }

                if (!found) return $"{i}{j}";
            }
        }

        return "ZZ";
    }
    
}
