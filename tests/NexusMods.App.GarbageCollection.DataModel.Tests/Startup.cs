using Microsoft.Extensions.DependencyInjection;
namespace NexusMods.App.GarbageCollection.DataModel.Tests;

public class Startup
{
    // https://github.com/pengweiqhca/Xunit.DependencyInjection?tab=readme-ov-file#3-closest-startup
    // A trick for parallelizing tests with Xunit.DependencyInjection
    public void ConfigureServices(IServiceCollection services) => DIHelpers.ConfigureServices(services);
}
