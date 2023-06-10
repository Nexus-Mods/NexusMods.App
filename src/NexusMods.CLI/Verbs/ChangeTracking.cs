using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Displays changes to the datastore waiting for each new change
/// </summary>
public class ChangeTracking : AVerb
{
    private readonly IRenderer _renderer;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="store"></param>
    public ChangeTracking(Configurator configurator, IDataStore store)
    {
        _renderer = configurator.Renderer;
        _store = store;
    }

    private readonly IDataStore _store;

    public static VerbDefinition Definition => new("change-tracking",
        "Display changes to the datastore waiting for each new change",
        Array.Empty<OptionDefinition>());

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

    private void HandleEvent(Table entity) => _renderer.Render(entity);
}
