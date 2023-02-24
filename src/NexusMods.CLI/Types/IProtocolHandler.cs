namespace NexusMods.CLI.Types;

public interface IProtocolHandler
{
    public string Protocol { get; }
    public Task Handle(string url, CancellationToken token);
}
