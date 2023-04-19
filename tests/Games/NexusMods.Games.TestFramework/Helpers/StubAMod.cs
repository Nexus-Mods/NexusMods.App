using JetBrains.Annotations;

namespace NexusMods.Games.TestFramework.Helpers;

/// <summary>
/// Contains routines for stubbing mods.
/// </summary>
/// <remarks>
///     Use these API/code to stub mods where the analyzed contents of the mod are irrelevant
///     for the given unit test; only file layout/order. etc.
///
///     The motivation here is to avoid downloading remote mods when possible, because that
///     operation can get slow and hold up unit tests as we scale to more and more mods.
/// </remarks>
[PublicAPI]
public static class StubAMod
{
    /// <summary>
    /// For each file inside the folder on the physical filesystem., sets the contents of the file to the name of the file.
    /// </summary>
    /// <param name="folderPath">Path to the folder.</param>
    /// <param name="exclude">If true, excludes this file from stubbing.</param>
    public static async Task SetFileContentsToFileNameAsync(string folderPath, Func<bool>? exclude = null)
    {
        exclude ??= () => false;
        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            if (!exclude())
                await File.WriteAllTextAsync(file, Path.GetFileName(file));
        }
    }
}
