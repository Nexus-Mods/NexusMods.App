using System.Buffers;
using System.Text;
using JetBrains.Annotations;
using MemoryPack;
using MemoryPack.Formatters;
using NexusMods.ProxyConsole.Exceptions;
using NexusMods.ProxyConsole.Messages;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.ProxyConsole;


/// <summary>
/// Serializes and deserializes messages to and from the server, and manages the underlying
/// duplex stream.
/// </summary>
public class Serializer
{
    private readonly BinaryWriter _binaryWriter;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly BinaryReader _binaryReader;

    /// <summary>
    /// Primary constructor, takes a duplex capable stream
    /// </summary>
    /// <param name="duplexStream"></param>
    /// <param name="renderableDefinitions"></param>
    public Serializer(Stream duplexStream, IEnumerable<IRenderableDefinition> renderableDefinitions)
    {
        var stream = duplexStream;
        _binaryWriter = new BinaryWriter(stream, Encoding.UTF8, true);
        _binaryReader = new BinaryReader(stream, Encoding.UTF8, true);
        _memoryPool = MemoryPool<byte>.Shared;

        if (!MemoryPackFormatterProvider.IsRegistered<IRenderable>())
        {
            // In the future we *could* redo this to use the guid as a tag, but since all this data is
            // ephemeral, it doesn't matter for now.
            var listDefs = renderableDefinitions
                .OrderBy(l => l.Id)
                .Select((def, idx) => ((ushort)idx, def.RenderableType));

            MemoryPackFormatterProvider.Register(new DynamicUnionFormatter<IRenderable>(listDefs.ToArray()));
        }
    }

    /// <summary>
    /// Sends the given message to the server and waits for an acknowledgement.
    /// </summary>
    /// <param name="msg"></param>
    /// <typeparam name="TMessage"></typeparam>
    [PublicAPI]
    public async Task SendAndAckAsync<TMessage>(TMessage msg)
    where TMessage : IMessage
    {
        await SendAsync(msg);
        await ReceiveExactlyAsync<Acknowledge>();
    }

    /// <summary>
    /// Sends the given message to the server and waits for a response of the given type.
    /// </summary>
    /// <param name="msg"></param>
    /// <typeparam name="TResponse"></typeparam>
    /// <typeparam name="TRequest"></typeparam>
    /// <returns></returns>
    public async Task<TResponse> SendAndReceiveAsync<TResponse, TRequest>(TRequest msg)
        where TResponse : class, IMessage
        where TRequest : IMessage
    {
        await SendAsync(msg);
        return await ReceiveExactlyAsync<TResponse>();
    }

    /// <summary>
    /// Sends the given message to the server and does not wait for a response.
    /// </summary>
    /// <param name="msg"></param>
    /// <typeparam name="TMessage"></typeparam>
    public async Task SendAsync<TMessage>(TMessage msg) where TMessage : IMessage
    {
        var serialized = MemoryPackSerializer.Serialize<IMessage>(msg);
        _binaryWriter.Write(serialized.Length);
        await _binaryWriter.BaseStream.WriteAsync(serialized);
    }

    /// <summary>
    /// Sends an acknowledgement to the server.
    /// </summary>
    [PublicAPI]
    public Task AcknowledgeAsync()
    {
        return SendAsync(new Acknowledge());
    }

    /// <summary>
    /// Receives a message of the given type from the server, if the message is not of the given type an
    /// exception is thrown.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task<TMessage> ReceiveExactlyAsync<TMessage>() where TMessage : class, IMessage
    {
        var deserialized = await ReceiveAsync();

        if (deserialized is not TMessage)
            UnexpectedMessageException.Throw(typeof(TMessage), deserialized?.GetType() ?? typeof(object));
        return (TMessage)deserialized!;
    }

    /// <summary>
    /// Receives the next message from the server.
    /// </summary>
    /// <returns></returns>
    public async Task<IMessage?> ReceiveAsync()
    {
        var size = _binaryReader.ReadUInt32();
        using var buffer = _memoryPool.Rent((int)size);
        var sized = buffer.Memory[..(int)size];
        await _binaryReader.BaseStream.ReadExactlyAsync(sized);
        var deserialized = MemoryPackSerializer.Deserialize<IMessage>(sized.Span);
        return deserialized;
    }
}
