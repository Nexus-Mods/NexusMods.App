using MemoryPack;

namespace NexusMods.ProxyConsole.Messages;

/// <summary>
/// A message that can be sent between the client and the server.
/// </summary>
[MemoryPackable]
[MemoryPackUnion(0x1, typeof(ProgramArgumentsRequest))]
[MemoryPackUnion(0x2, typeof(ProgramArgumentsResponse))]
[MemoryPackUnion(0x3, typeof(Render))]
[MemoryPackUnion(0x4, typeof(Clear))]
[MemoryPackUnion(ushort.MaxValue, typeof(Acknowledge))]
public partial interface IMessage
{

}
