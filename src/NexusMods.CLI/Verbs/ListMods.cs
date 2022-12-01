namespace NexusMods.CLI.Verbs;

public class ListMods
{
    public static VerbDefinition Definition = new VerbDefinition("list-mods",
        "List all the mods in a given managed game",
        new[]
        {
            new OptionDefinition(typeof(string), "m", "managedGame", "The managed game to access")
        });
    public Delegate Run { get; set; }
}