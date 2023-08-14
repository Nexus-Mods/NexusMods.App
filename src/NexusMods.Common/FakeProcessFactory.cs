using System.Buffers;
using System.Diagnostics;
using System.Text;
using CliWrap;

namespace NexusMods.Common;

public class FakeProcessFactory : IProcessFactory
{
    private readonly int _exitCode;

    public Action<Command>? Callback { get; set; }
    public Func<Command, Task>? AsyncCallback { get; set; }

    public string? StandardOutput { get; set; }
    public string? StandardError { get; set; }

    public List<Command> Commands { get; } = new();

    public FakeProcessFactory(int exitCode)
    {
        _exitCode = exitCode;
    }

    public async Task<CommandResult> ExecuteAsync(Command command,
        CancellationToken cancellationToken = default)
    {
        Callback?.Invoke(command);
        if (AsyncCallback is not null) await AsyncCallback.Invoke(command);

        if (StandardOutput is not null)
        {
            await WriteStringToPipe(
                StandardOutput,
                command.StandardOutputPipe,
                cancellationToken);
        }

        if (StandardError is not null)
        {
            await WriteStringToPipe(
                StandardError,
                command.StandardErrorPipe,
                cancellationToken);
        }

        return await Task.FromResult(new CommandResult(
            _exitCode,
            DateTimeOffset.Now,
            DateTimeOffset.Now));
    }

    /// <inheritdoc />
    public Process? ExecuteAndDetach(Command command)
    {
        throw new ExecuteAndDetatchException(command);
    }

    /// <summary>
    /// Thrown when <see cref="IProcessFactory.ExecuteAndDetach"/> is called. Used to test that the
    /// correct command was passed to the factory.
    /// </summary>
    public class ExecuteAndDetatchException : Exception
    {
        public Command Command { get; }
        public ExecuteAndDetatchException(Command command) : base("Executed command and detached from it")
        {
            Command = command;
        }
    }

    private static async Task WriteStringToPipe(string text, PipeTarget pipe, CancellationToken cancellationToken = default)
    {
        var bytes = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(text.Length));
        var numBytes = Encoding.UTF8.GetBytes(text, bytes);

        using var ms = new MemoryStream(bytes, 0, numBytes, false);
        await pipe.CopyFromAsync(ms, cancellationToken);
    }
}
