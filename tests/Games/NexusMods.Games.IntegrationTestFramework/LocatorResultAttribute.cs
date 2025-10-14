using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Paths;

namespace NexusMods.Games.IntegrationTestFramework;

public abstract class LocatorResultAttribute<TGame> : DataSourceGeneratorAttribute<Type, GameLocatorResult> 
    where TGame : IGame
{
    protected abstract IEnumerable<GameLocatorResult> GetLocatorResults();

    protected override IEnumerable<Func<(Type, GameLocatorResult)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        foreach (var result in GetLocatorResults())
        {
            yield return () => (typeof(TGame), result);
        }
    }
}
