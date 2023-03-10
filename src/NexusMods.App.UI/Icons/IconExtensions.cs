using System.Reflection;

namespace NexusMods.App.UI.Icons;



public static class IconExtensions
{
    private static readonly Dictionary<IconType, string> _materialNames = Enum.GetValues<IconType>()
        .Select(v => (v, v.GetType().GetField(v.ToString())!.GetCustomAttribute<MaterialNameAttribute>()))
        .ToDictionary(v => v.Item1, v => v.Item2?.Name ?? string.Empty);

    public static string ToMaterialUIName(this IconType type)
    {
        return _materialNames[type];
    }
}
