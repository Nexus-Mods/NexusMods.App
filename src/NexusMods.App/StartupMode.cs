namespace NexusMods.App;

public class StartupMode
{
    /// <summary>
    /// The CLI arguments
    /// </summary>
    public string[] Args { get; set; } = [];
    
    /// <summary>
    /// The original unprocessed CLI arguments
    /// </summary>
    public string[] OriginalArgs { get; set; } = [];
    
    /// <summary>
    /// If true, the app will assume it is the main application and will not try to connect to another instance
    /// </summary>
    public bool RunAsMain { get; set; } = false;
    
    /// <summary>
    /// If true, the app will execute the CLI commands, otherwise the arguments will be ignored
    /// </summary>
    public bool ExecuteCli { get; set; } = false;
    
    /// <summary>
    /// If true, the Avalonia UI will be shown
    /// </summary>
    public bool ShowUI { get; set; } = true;
    
    public bool IsAvaloniaDesigner { get; set; } = false;
    
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
