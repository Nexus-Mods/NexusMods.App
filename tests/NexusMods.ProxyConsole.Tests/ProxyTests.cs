using FluentAssertions;
using NexusMods.Sdk.ProxyConsole;
using Table = NexusMods.Sdk.ProxyConsole.Table;

namespace NexusMods.ProxyConsole.Tests;

public class ProxyTests(IServiceProvider provider) : AProxyConsoleTest(provider)
{
    [Fact]
    public async Task CanRenderTest()
    {
        await Server.RenderAsync(new Text {Template = "Hello World!"});
        await Task.Delay(100);

        LoggingRenderer.Messages.Should().BeEquivalentTo(new List<(string Method, object Message)>
        {
            ("RenderAsync", new Text {Template = "Hello World!"})
        });
    }

    [Fact]
    public async Task CanRenderMultipleTexts()
    {
        await Server.RenderAsync(new Text {Template = "Hello World!1"});
        await Server.RenderAsync(new Text {Template = "Hello World!2"});
        await Server.RenderAsync(new Text {Template = "Hello World!3"});
        await Task.Delay(100);


        LoggingRenderer.Messages.Should().BeEquivalentTo(new List<(string Method, object Message)>
        {
            ("RenderAsync", new Text {Template = "Hello World!1"}),
            ("RenderAsync", new Text {Template = "Hello World!2"}),
            ("RenderAsync", new Text {Template = "Hello World!3"})
        });
    }

    [Fact]
    public async Task CanRenderTable()
    {
        await Server.RenderAsync(new Table
        {
            Columns =
            [
                new Text {Template = "Text"},
                new Text {Template = "Number"},
            ],
            Rows =
            [
                new IRenderable[] { new Text { Template = "Hello World!1" } , new Text { Template = "4" }},
                new IRenderable[] { new Text { Template = "Hello World!2" } , new Text { Template = "5" }},
            ]
        });

        await Task.Delay(100);

        LoggingRenderer.Messages.Should().BeEquivalentTo(new List<(string Method, object Message)>
        {
            ("RenderAsync",
                new Table
                {
                    Columns =
                    [
                        new Text {Template = "Text"},
                        new Text {Template = "Number"},
                    ],
                    Rows =
                    [
                        new IRenderable[] { new Text { Template = "Hello World!1" }, new Text { Template = "4" }},
                        new IRenderable[] { new Text { Template = "Hello World!2" }, new Text { Template = "5" }},
                    ],
                }),
        });
    }


}
