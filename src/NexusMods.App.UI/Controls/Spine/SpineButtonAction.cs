using NexusMods.DataModel.Games;

namespace NexusMods.App.UI.Controls.Spine;

public enum Type
{
    Home,
    Game,
    Add,
    Download
}

public record SpineButtonAction(Type Type, IGame? Game = null);
