using NexusMods.DataModel.Abstractions;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
public class ChangeTracking : AVerb
{
    private readonly IRenderer _renderer;
    
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
        _store.Changes.Subscribe(s =>
        HandleEvent(s.Entity));
        
        while (!token.IsCancellationRequested)
            await Task.Delay(1000, token);
        return 0;
    }

    private void HandleEvent(Entity entity) => _renderer.Render(entity);
}