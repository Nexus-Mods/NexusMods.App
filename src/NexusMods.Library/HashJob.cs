using DynamicData.Kernel;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Library;

internal class HashJob : AJob
{
    public HashJob(IJobGroup? group = default, Optional<IJobWorker> worker = default)
        : base(new MutableProgress(new IndeterminateProgress()), group, worker) { }

    private Stream? _stream;
    public required Stream Stream
    {
        get => _stream ?? throw new ObjectDisposedException("This job has already been disposed!");
        init => _stream = value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_stream is not null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_stream is not null)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
            _stream = null;
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}
