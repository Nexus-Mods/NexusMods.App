using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.LoadoutSynchronizer;

namespace NexusMods.DataModel.Tests.LoadoutSynchronizerTests;

public class LoadoutSynchronizerStub : ALoadoutSynchronizer
{
    protected LoadoutSynchronizerStub(ILogger logger) : base(logger) { }

    public static LoadoutSynchronizerStub Create(ILogger logger)
    {
        return new LoadoutSynchronizerStub(logger);
    }
}
