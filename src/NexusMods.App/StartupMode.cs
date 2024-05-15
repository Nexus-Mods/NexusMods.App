namespace NexusMods.App;

public class StartupMode
{
    public string[] Args { get; set; } = [];
    public string[] OriginalArgs { get; set; } = [];
    public bool RunAsMain { get; set; } = false;
    
    public bool ExecuteCli { get; set; } = false;
    public bool ShowUI { get; set; } = true;
    
    public static StartupMode Parse(string[] args)
    {
        var mode = new StartupMode
        {
            Args = args, 
            OriginalArgs = args, 
            ExecuteCli = true,
        };

        if (args.Length == 0)
        {
            mode.ShowUI = true;
            mode.RunAsMain = true;
            mode.ExecuteCli = false;
        }
        else switch (args[0])
        {
            case "as-main":
                mode.RunAsMain = true;
                mode.ShowUI = false;
                mode.Args = args[1..];
                mode.ExecuteCli = args.Length > 1;
                break;
            case "as-main-ui":
                mode.RunAsMain = true;
                mode.ShowUI = true;
                mode.Args = args[1..];
                mode.ExecuteCli = args.Length > 1;
                break;
            default:
                mode.RunAsMain = false;
                mode.ShowUI = false;
                break;
        }
        return mode;
    }
}
