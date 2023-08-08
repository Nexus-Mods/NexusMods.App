using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Displays changes to the datastore waiting for each new change
/// </summary>
public class ChangeTracking : AVerb, IRenderingVerb
{

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="store"></param>
    public ChangeTracking(IDataStore store)
    {
        _store = store;
    }

    private readonly IDataStore _store;

    /// <inheritdoc />
    public static VerbDefinition Definition => new("change-tracking",
        "Display changes to the datastore waiting for each new change",
        Array.Empty<OptionDefinition>());

    /// <inheritdoc />
    public async Task<int> Run(CancellationToken token)
    {
        using var _ = _store.IdChanges.Subscribe(id =>
        HandleEvent(new Table(new[] { "Id" }, new[]
        {
            new object[] {id},
        })));

        while (!token.IsCancellationRequested)
            await Task.Delay(1000, token);
        return 0;
    }

    private void HandleEvent(Table entity) => Renderer.Render(entity);
}
