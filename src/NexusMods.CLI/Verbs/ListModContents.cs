namespace NexusMods.CLI.Verbs;

public class ListModContents
{
    public static VerbDefinition Definition = new VerbDefinition("list-mod-conetents", "Lists all the files in a mod",
        new[]
        {
            new OptionDefinition<string>( "m", "managedGame", "The managed game instance that contains the mod"),
            new OptionDefinition<string>("n", "modName", "The name of the mod to list")
        });
    public Delegate Run { get; set; }
}