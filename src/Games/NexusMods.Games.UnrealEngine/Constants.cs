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
    /// Relative to <see cref="GameMainUE"/>
    /// </summary>
    public static readonly GamePath ContentModsPath = new(GameMainUE, "Content/Paks/~mods");
    /// <summary>
    /// Relative to <see cref="GameMainUE"/>
    /// </summary>
    public static readonly GamePath InjectorModsPath = new(GameMainUE, "Binaries/Win64");
    /// <summary>
    /// Relative to <see cref="LocationId.AppData"/>
    /// </summary>
    public static readonly GamePath ConfigPath = new(LocationId.AppData, "Saved/Config");
    /// <summary>
    /// Relative to <see cref="LocationId.AppData"/>
    /// </summary>
    public static readonly GamePath LogsPath = new(LocationId.AppData, "Saved/Logs");
    /// <summary>
    /// Relative to <see cref="LocationId.AppData"/>
    /// </summary>
    public static readonly GamePath CrashesPath = new(LocationId.AppData, "Saved/Crashes");

    public static readonly Extension PakExt = new(".pak");
    public static readonly Extension UcasExt = new(".ucas");
    public static readonly Extension UtocExt = new(".utoc");
    public static readonly Extension SigExt = new(".sig");
    public static readonly Extension DLLExt = new(".dll");
    public static readonly Extension ExeExt = new(".exe");
    public static readonly Extension ConfigExt = new(".ini");
    public static readonly Extension SavedGameExt = new(".sav");

    public static readonly HashSet<Extension> ContentExts = [PakExt, UcasExt, UtocExt, SigExt];
    public static readonly HashSet<Extension> ArchiveExts = [new(".zip"), new(".rar")];

    [GeneratedRegex(@"^(?<modName>.+?)-(?<id>\d+)-(?<version>(?:\d+-?)+)-(?<uniqueId>\d+)\.(zip|rar)$", RegexOptions.IgnoreCase)]
    public static partial Regex DefaultUEModArchiveNameRegex();

    [GeneratedRegex(@"^(?<modName>.*?)\s?[-_]?\s?(?<version>[\d.]+)\.(zip|rar)$", RegexOptions.IgnoreCase)]
    public static partial Regex ModArchiveNameRegexFallback();

    [GeneratedRegex(@"[\w/]+\.(uasset|uexp|ubulk)", RegexOptions.IgnoreCase)]
    public static partial Regex UEObjectsRegex();
}
