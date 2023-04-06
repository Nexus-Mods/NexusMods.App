using System.ComponentModel;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.RateLimiting;

namespace NexusMods.DataModel.Interprocess.Messages;

public interface IInterprocessJob : INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// The OS level processId of the process that created the job. If this process
    /// cannot be found, the job will be considered orphaned and will be removed.
    /// </summary>
    public ProcessId ProcessId { get; }

    /// <summary>
    /// Unique identifier of the job.
    /// </summary>
    public JobId JobId { get; }

    /// <summary>
    /// How far along the job is.
    /// </summary>
    public Percent Progress { get; set; }

    /// <summary>
    /// English description of the job.
    /// </summary>
    public string Description { get; }

    public JobType JobType { get; }

    public DateTime StartTime { get; }

    public byte[] Data { get; }

    /// <summary>
    /// Returns the payload as a <see cref="IId"/>. This is only valid if the payload
    /// is a <see cref="IId"/>.
    /// </summary>
    IId PayloadAsId { get; }

    /// <summary>
    /// Returns the payload as a Uri (utf-8 encoded string)
    /// </summary>
    Uri PayloadAsUri { get; }

    /// <summary>
    /// Returns the payload as a <see cref="ILoadoutId"/>. This is only valid if the payload
    /// </summary>
    LoadoutId LoadoutId { get; }

    /// <summary>
    /// Returns the payload as a <see cref="IMessage"/>. This is only valid if the payload is a <see cref="IMessage"/>
    /// of the specified type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T PayloadAsIMessage<T>() where T : IMessage, new()
    {
        return (T)T.Read(Data);
    }
}
