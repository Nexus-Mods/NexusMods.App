using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Banners;

public sealed class BannerSettingsWrapper : ReactiveObject
{
    private readonly ISettingsManager? _settingsManager;

    public string Key { get; }

    [Reactive] public bool IsDismissed { get; private set; }

    public BannerSettingsWrapper()
    {
        _settingsManager = null;
        Key = string.Empty;
    }

    public BannerSettingsWrapper(ISettingsManager settingsManager, string key)
    {
        _settingsManager = settingsManager;

        Key = key;
        IsDismissed = settingsManager.Get<BannerSettings>().IsDismissed(key);
    }

    public void DismissBanner()
    {
        IsDismissed = true;

        _settingsManager?.Update<BannerSettings>(bannerSettings => bannerSettings with
        {
            BannerStatus = bannerSettings.BannerStatus.SetItem(Key, true),
        });
    }
}
