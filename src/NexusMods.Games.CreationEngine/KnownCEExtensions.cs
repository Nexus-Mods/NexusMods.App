using NexusMods.Paths;

namespace NexusMods.Games.CreationEngine;

public static class KnownCEExtensions
{
    public static readonly Extension BSA = new Extension(".bsa");
    public static readonly Extension BA2 = new Extension(".ba2");
    public static readonly Extension ESM = new Extension(".esm");
    public static readonly Extension ESP = new Extension(".esp");
    public static readonly Extension ESL = new Extension(".esl");
    
    public static readonly Extension[] PluginFiles = [ESM, ESP, ESL];
}
