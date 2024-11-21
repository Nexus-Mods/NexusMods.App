using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Games.FOMOD;

public static class EmptyDirectory
{
    private const string Namespace = "NexusMods.Games.FOMOD.EmptyDirectory";
    
    public static readonly BooleanAttribute Directory = new(Namespace, nameof(EmptyDirectory));
}
