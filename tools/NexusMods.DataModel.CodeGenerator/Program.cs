using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.CodeGenerator;

namespace Wabbajack.DTOs.ConverterGenerators;

internal class Program
{
    private static void Main(string[] args)
    {
        var cfile = new CFile();
        new PolymorphicGenerator<Entity>().GenerateAll(cfile);

        cfile.Write(@"..\..\src\NexusMods.DataModel\JsonConverters\Generated.cs");
    }
}