using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Sorting;

namespace NexusMods.DataModel.CodeGenerator;

public class Program
{
    public static void Main(string[] args)
    {
        var cfile = new CFile();
        new PolymorphicGenerator(typeof(Entity)).GenerateAll(cfile);
        new PolymorphicGenerator(typeof(ISortRule<,>)).GenerateAll(cfile);

        if (args.Any() && args[0] == "dryrun")
            return;
        
        cfile.Write(@"..\..\src\NexusMods.DataModel\JsonConverters\Generated.cs");
    }
}