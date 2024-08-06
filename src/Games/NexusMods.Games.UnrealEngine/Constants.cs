using System.Text.RegularExpressions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Games.UnrealEngine;
public static partial class Constants
{
    /// <summary>
    /// {Game}/{GameMainUE}/Content/Paks
    /// </summary>
    public static readonly LocationId GameMainUE = LocationId.From("GameMainUE");
    /// <summary>
    /// Relative to GameMainUE
    /// </summary>
    public static readonly RelativePath ContentModsPath = "Content/Paks/~mods".ToRelativePath();
    /// <summary>
    /// Relative to GameMainUE
    /// </summary>
    public static readonly RelativePath InjectorModsPath = "Binaries/Win64".ToRelativePath();
    public static readonly Extension PakExt = new(".pak");
    public static readonly Extension UcasExt = new(".ucas");
    public static readonly Extension UtocExt = new(".utoc");
    public static readonly Extension SigExt = new(".sig");
    public static readonly Extension DLLExt = new(".dll");
    public static readonly Extension ExeExt = new(".exe");
    public static readonly Extension ConfigExt = new(".ini");

    public static readonly HashSet<Extension> ContentExts = [PakExt, UcasExt, UtocExt, SigExt];
    public static readonly HashSet<Extension> ArchiveExts = [new(".zip"), new(".rar")];

    [GeneratedRegex(@"^(?<modName>.+?)-(?<id>\d+)-(?<version>(?:\d+-?)+)-(?<uniqueId>\d+)\.(zip|rar)$", RegexOptions.IgnoreCase)]
    public static partial Regex DefaultUEModArchiveNameRegex();

    [GeneratedRegex(@"^(?<modName>.*?)\s?[-_]?\s?(?<version>[\d.]+)\.(zip|rar)$", RegexOptions.IgnoreCase)]
    public static partial Regex ModArchiveNameRegexFallback();
}
