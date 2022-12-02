using NexusMods.DataModel.CodeGenerator;

namespace NexusMods.DataModel.Tests;

public class GeneratorTests
{
    [Fact]
    public void GeneratorRuns()
    {
        Program.Main(new []{"dryrun"});
    }
}