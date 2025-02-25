using NexusMods.Paths;
using NexusMods.Abstractions.GameLocators;
using System.Collections.Immutable;

namespace NexusMods.Games.UnrealEngine;

public static partial class Constants
{
    public static readonly LocationId GameMainLocationId = LocationId.From("UE_GameMain");
    public static readonly LocationId PakModsLocationId = LocationId.From("UE_PakMods");
    public static readonly LocationId LogicModsLocationId = LocationId.From("UE_LogicMods");
    public static readonly LocationId LuaModsLocationId = LocationId.From("UE_LuaMods");
    public static readonly LocationId BinariesLocationId = LocationId.From("UE_Binaries");
    public static readonly LocationId ConfigLocationId = LocationId.From("UE_ConfigPath");

    public static readonly GamePath EnginePath = new(GameMainLocationId, "Engine");
    public static readonly GamePath ResourcesPath = new(GameMainLocationId, "Resources");

    public static readonly Extension ExeExt = new(".exe");
    public static readonly Extension DllExt = new(".dll");
    public static readonly Extension SaveExt = new(".sav");
    public static readonly Extension ConfigExt = new(".ini");
    public static readonly Extension LuaExt = new(".lua");
    public static readonly Extension TxtExt = new(".txt");
    public static readonly Extension PakExt = new(".pak");
    public static readonly Extension SigExt = new(".sig");
    public static readonly Extension UassetExt = new(".uasset");
    public static readonly Extension UexpExt = new(".uexp");
    public static readonly Extension UbulkExt = new(".ubulk");
    public static readonly Extension UcasExt = new(".ucas");
    public static readonly Extension UtocExt = new(".utoc");

    public static readonly string ScriptingSystemFileName = "dwmapi.dll";

    public static readonly ImmutableHashSet<Extension> ContentExts = ImmutableHashSet.Create(PakExt, UcasExt, UtocExt);
}
