﻿using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.App.UI.Routing.Messages;

/// <summary>
/// Tells the app to route the display to the given loadout.
/// </summary>
/// <param name="Loadout"></param>
public record NavigateToLoadout(Loadout Loadout) : IRoutingMessage
{
    public IGame Game => Loadout.Installation.Game;
    public LoadoutId LoadoutIdId => Loadout.LoadoutId;
    public GameInstallation GameInstallation => Loadout.Installation;
}
