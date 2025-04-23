using System.Buffers;
using System.Text;
using CliWrap;

namespace NexusMods.CrossPlatform.Process;

internal class FakeProcessFactory : IProcessFactory
{
    private readonly int _exitCode;

    public Action<Command>? Callback { get; set; }
    public Func<Command, Task>? AsyncCallback { get; set; }

    public string? StandardOutput { get; set; }
    public string? StandardError { get; set; }

    public FakeProcessFactory(int exitCode)
    {
        _exitCode = exitCode;
    }

    public async Task<CommandResult> ExecuteAsync(
        Command command,
        bool logProcessOutput = true,
        bool validateExitCode = false,
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

    public Task ExecuteProcessAsync(System.Diagnostics.Process process, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private static async Task WriteStringToPipe(string text, PipeTarget pipe, CancellationToken cancellationToken = default)
    {
        var bytes = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(text.Length));
        var numBytes = Encoding.UTF8.GetBytes(text, bytes);

        using var ms = new MemoryStream(bytes, 0, numBytes, false);
        await pipe.CopyFromAsync(ms, cancellationToken);
    }
}
