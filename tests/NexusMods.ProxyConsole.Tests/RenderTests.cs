using FluentAssertions;
using NexusMods.Sdk.ProxyConsole;
using Spectre.Console.Testing;

namespace NexusMods.ProxyConsole.Tests;

public class RenderTests
{
    private readonly TestConsole _test;
    private readonly SpectreRenderer _renderer;

    public RenderTests()
    {
        _test = new TestConsole();
        _renderer = new SpectreRenderer(_test);

    }

    [Fact]
    public async Task CanRenderText()
    {
        await _renderer.RenderAsync(new Text {Template = "Hello World!"});

        _test.Output.Should().Be("Hello World!");
    }

    [Fact]
    public async Task CanRenderMultipleTexts()
    {
        await _renderer.RenderAsync(new Text {Template = "Hello World!1"});
        await _renderer.RenderAsync(new Text {Template = "Hello World!2"});
        await _renderer.RenderAsync(new Text {Template = "Hello World!3"});

        _test.Output.Should().Be("Hello World!1Hello World!2Hello World!3");
    }
}
