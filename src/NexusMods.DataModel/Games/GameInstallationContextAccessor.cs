using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

public sealed record GameInstallationContext(AbsolutePath InstallationPath, GameStore GameStore);

public sealed class GameInstallationContextAccessor
{
    public GameInstallationContext? GameInstalltionContext { get; set; }

    private static readonly AsyncLocal<GameInstallationContextHolder> _gameInstallationContextCurrent = new();

    public GameInstallationContext? GameInstallationContext
    {
        get => _gameInstallationContextCurrent.Value?.Context;
        set
        {
            var holder = _gameInstallationContextCurrent.Value;
            if (holder != null)
            {
                // Clear current GameInstallationContext trapped in the AsyncLocals, as its done.
                holder.Context = null;
            }

            if (value != null)
            {
                // Use an object indirection to hold the GameInstallationContext in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared.
                _gameInstallationContextCurrent.Value = new GameInstallationContextHolder { Context = value };
            }
        }
    }

    private sealed class GameInstallationContextHolder
    {
        public GameInstallationContext? Context;
    }
}
