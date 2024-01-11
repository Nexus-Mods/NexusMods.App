namespace NexusMods.Games.MountAndBlade2Bannerlord.Options;

public sealed class MountAndBlade2BannerlordOptions
{
    public bool FixCommonIssues { get; set; }
    public bool UnblockFiles { get; set; }
    public bool BetaSorting { get; set; }

    public bool DisableBinaryCheck { get; set; }

    public bool DisableCrashHandlerWhenDebuggerIsAttached { get; set; }

    public bool DisableCatchAutoGenExceptions { get; set; }

    public bool UseVanillaCrashHandler { get; set; }
}
