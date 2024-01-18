using NexusMods.Abstractions.Games.Loadouts;

namespace NexusMods.DataModel.Tests;

public class LoadoutSynchronizerStub : ALoadoutSynchronizer
{
    protected LoadoutSynchronizerStub(IServiceProvider logger) : base(logger) { }

    public static LoadoutSynchronizerStub Create(IServiceProvider provider)
    {
        return new LoadoutSynchronizerStub(provider);
    }
}
