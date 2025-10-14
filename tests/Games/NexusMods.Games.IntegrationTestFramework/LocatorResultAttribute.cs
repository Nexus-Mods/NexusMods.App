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
        if (Environment.GetEnvironmentVariable("NMA_INTEGRATION_BASE_PATH") == null)
        {
            Console.WriteLine("NMA_INTEGRATION_BASE_PATH environment variable is not set, skipping integration tests");
            yield break;
        }

        foreach (var result in GetLocatorResults())
        {
            yield return () => (typeof(TGame), result);
        }
    }
}
