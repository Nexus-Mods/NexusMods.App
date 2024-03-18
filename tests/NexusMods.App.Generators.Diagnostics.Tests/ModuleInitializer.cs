using System.Runtime.CompilerServices;

namespace NexusMods.App.Generators.Diagnostics.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
