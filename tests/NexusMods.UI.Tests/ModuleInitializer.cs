using System.Runtime.CompilerServices;
using ImageMagick;

namespace NexusMods.UI.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifyImageMagick.Initialize();
        VerifyImageMagick.RegisterComparers(threshold: 0.005D, metric: ErrorMetric.Fuzz);
    }

    [ModuleInitializer]
    public static void InitOther()
    {
        VerifierSettings.InitializePlugins();
    }
}
