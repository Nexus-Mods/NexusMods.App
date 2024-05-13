using System.Threading;
using System.Threading.Tasks;
using NexusMods.ProxyConsole.Abstractions;

namespace NexusMods.SingleProcess;

/// <summary>
/// A handler that will be called when a client connects to the main process.
/// </summary>
public interface IMainProcessHandler
{
    /// <summary>
    /// Handle a new client connection to the server. This method should return when the client disconnects.
    /// This is singnaled by the token being cancelled.
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="console"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task HandleAsync(string[] arguments, IRenderer console, CancellationToken token);
}
