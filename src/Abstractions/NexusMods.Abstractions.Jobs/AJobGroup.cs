using System.Collections;
using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public abstract class AJobGroup : AJob, IJobGroup
{
    private readonly List<IJob> _collection;
    private readonly ObservableCollection<IJob> _observableCollection;

    protected AJobGroup(
        IJobGroup? group = default,
        IJobWorker? worker = default) : base(CreateGroupProgress(), group, worker)
    {
        _collection = [];
        _observableCollection = new ObservableCollection<IJob>(_collection);
        ObservableCollection = new ReadOnlyObservableCollection<IJob>(_observableCollection);
    }

    public IEnumerator<IJob> GetEnumerator() => _collection.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();
    public int Count => _observableCollection.Count;

    public IJob this[int index] => _collection[index];

    public ReadOnlyObservableCollection<IJob> ObservableCollection { get; }

    internal void AddJob(AJob job)
    {
        // TODO: sanity checks and other stuff
        _observableCollection.Add(job);
    }

    private static MutableProgress CreateGroupProgress()
    {
        // TODO: figure out what to use here
        throw new NotImplementedException();
    }
}
