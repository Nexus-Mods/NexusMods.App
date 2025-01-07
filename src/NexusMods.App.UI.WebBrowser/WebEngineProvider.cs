using DotNetBrowser.Browser;
using DotNetBrowser.Engine;

namespace NexusMods.App.UI.WebBrowser;

public static class WebEngineProvider
{
    private static readonly IEngine _engine;

    static WebEngineProvider()
    {
        _engine = EngineFactory.Create(new EngineOptions.Builder
            {
                // Only for testing purposes
                LicenseKey = Environment.GetEnvironmentVariable("DOTNNET_BROWSER_KEY"),
            }.Build()
        );
    }
    
    public static IBrowser GetBrowser()
    {
        return _engine.CreateBrowser();
    }
}
