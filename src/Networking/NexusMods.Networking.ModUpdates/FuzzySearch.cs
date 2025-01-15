using System.Text.RegularExpressions;
namespace NexusMods.Networking.ModUpdates;

/// <summary>
/// This class has all the code around the 'fuzzy search' feature documented in
/// `0019-updating-mods.md` page of the wiki.
/// </summary>
public static class FuzzySearch
{
    private static readonly string[] KnownFileExtensions =
    [
        ".rar", ".zip", ".7z", ".exe", ".omod", ".nx",
    ];
    
    /// <summary>
    /// Normalizes a filename by:
    /// 1. Stripping all possible version number variants
    /// 2. Replacing underscores with spaces
    /// 3. Stripping file extensions
    /// 4. Converting to lowercase for case-insensitive comparison
    /// </summary>
    /// <param name="fileName">The filename to normalize</param>
    /// <param name="version">The version to strip from the filename</param>
    /// <returns>The normalized filename</returns>
    public static string NormalizeFileName(string? fileName, string? version)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var result = fileName;
        
        // Strip all possible version number variants
        if (!string.IsNullOrWhiteSpace(version))
        {
            // Sort by length, to ensure we don't match a substring containing another string
            var versionVariants = GetVersionPermutations(version).OrderByDescending(x => x.Length);
            foreach (var variant in versionVariants)
                result = result.Replace(variant, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        // Replace underscores with spaces
        result = result.Replace('_', ' ');
        
        // Strip extensions
        result = StripFileExtension(result);
        
        // Normalize whitespace (trim and reduce multiple spaces to single)
        result = string.Join(" ", result.Split([' '], 
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            
        // Convert to lowercase for case-insensitive comparison
        return result.ToLowerInvariant();
    }
    
    /// <summary>
    /// Given an existing version string, return all possible variations that
    /// a user may place inside the file name on a mod page.
    ///
    /// Refer to tests of this for examples.
    /// </summary>
    /// <param name="version">The original version string attached</param>
    public static string[] GetVersionPermutations(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return [];

        var results = new HashSet<string>(8);
        void AddIfValid(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                results.Add(value);
        }

        void AddVariants(string input)
        {
            AddIfValid(input);
            AddIfValid(input.Replace(".", "_"));
        }

        // Add original version and its variants
        AddVariants(version);

        // Remove version suffix.
        var withoutTextSuffix = version;
        for (var x = version.Length - 1; x >= 0; x--)
        {
            if (!char.IsNumber(version[x])) continue;
            withoutTextSuffix = version.Substring(0, x + 1);
            break;
        }
        
        if (withoutTextSuffix != version)
            AddVariants(withoutTextSuffix);
            
        // Handle Single letter version prefixes variants ('v' for version, 'a' for 'alpha', 'b' for 'beta', etc.)
        if (version.Length > 0 && char.IsLetter(version[0]))
        {
            var withoutPrefix = version.Substring(1);
            AddVariants(withoutPrefix);
            
            // Also add variants without both prefix and suffix
            if (withoutTextSuffix != version)
                AddVariants(withoutPrefix.Substring(0, withoutTextSuffix.Length - 1));
        }

        return results.ToArray();
    }
    
    /// <summary>
    /// Strips known file extensions from the end of a filename
    /// </summary>
    /// <param name="fileName">The filename to process</param>
    /// <returns>Filename with any known extension removed</returns>
    public static string StripFileExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return fileName;
            
        foreach (var ext in KnownFileExtensions)
        {
            if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return fileName[..^ext.Length];
        }
        
        return fileName;
    }
}
