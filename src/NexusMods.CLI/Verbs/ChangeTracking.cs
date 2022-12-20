﻿using System.ComponentModel;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.CLI.Verbs;

public class ChangeTracking
{
    private readonly IRenderer _renderer;
    
    public ChangeTracking(Configurator configurator, IDataStore store)
    {
        _renderer = configurator.Renderer;
        _store = store;
    }
    
    private readonly IDataStore _store;

    
    public static VerbDefinition Definition = new("change-tracking",
        "Display changes to the datastore waiting for each new change",
        Array.Empty<OptionDefinition>());
    
    public async Task Run(CancellationToken token)
    {
        _store.Changes.Subscribe(s =>
        HandleEvent(s.Id, s.Entity));
        
        while (!token.IsCancellationRequested)
            await Task.Delay(1000, token);
    }

    public void HandleEvent(Id id, Entity entity)
    {
        _renderer.Render(entity);
    }
}