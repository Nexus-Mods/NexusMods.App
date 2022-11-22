namespace NexusMods.CLI.Tests;

public class CLITests
{
    private readonly CommandLineBuilder _builder;
    private readonly LoggingRenderer _renderer;

    public CLITests(CommandLineBuilder builder, LoggingRenderer renderer)
    {
        _builder = builder;
        _renderer = renderer;
    }
    
    [Fact]
    public async Task CanListGames()
    {
        _renderer.Reset();
        Assert.Equal(0, await _builder.Run(new[] { "list-games" }));
        
        Assert.NotEmpty(_renderer.Logged);
    }
}