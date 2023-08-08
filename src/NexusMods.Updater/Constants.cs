using NexusMods.Paths;

namespace NexusMods.Updater;

public static class Constants
{
    public static RelativePath UpdateMarkerFile = new("UPDATE_READY");
    public static RelativePath UpdateFolder = new("__update__");
    public static RelativePath UpdateExecutable = new("NexusMods.App.exe");

}
