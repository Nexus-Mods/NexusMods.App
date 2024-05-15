using MemoryPack;

namespace NexusMods.ProxyConsole.Messages;

[MemoryPackable]
public partial class ProgramArgumentsResponse : IMessage
{
    public required string[] Arguments { get; init; }

}
