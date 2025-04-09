using Newtonsoft.Json;

namespace NexusMods.Games.UnrealEngine.SortOrder;

public class LuaJsonEntry
{
    [JsonProperty("mod_name")]
    public required string ModName { get; set; }

    [JsonProperty("mod_enabled")]
    public required bool ModEnabled { get; set; }
}
