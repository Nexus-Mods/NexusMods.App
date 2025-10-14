namespace NexusMods.Games.IntegrationTestFramework;

public class IntegrationTestDataSource : DependencyInjectionDataSourceAttribute<IServiceProvider>
{
    public override IServiceProvider CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        throw new NotImplementedException();
    }

    public override object? Create(IServiceProvider scope, Type type)
    {
        throw new NotImplementedException();
    }
}
