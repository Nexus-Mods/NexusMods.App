using Bannerlord.LauncherManager.External;
using Bannerlord.LauncherManager.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Providers;

// The Load Order screen
internal sealed class LoadOrderStateProvider : ILoadOrderStateProvider
{

    public IModuleViewModel[]? GetAllModuleViewModels() => null;

    public IModuleViewModel[]? GetModuleViewModels() => null;

    public void SetModuleViewModels(IReadOnlyList<IModuleViewModel> moduleViewModels) { }
}
