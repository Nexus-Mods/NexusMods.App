using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Alerts;

public sealed class AlertSettings : ReactiveObject
{
    private readonly ISettingsManager? _settingsManager;

    public string Key { get; }

    [Reactive] public bool IsDismissed { get; private set; }

    public AlertSettings()
    {
        _settingsManager = null;
        Key = string.Empty;
    }

    public AlertSettings(ISettingsManager settingsManager, string key)
    {
        _settingsManager = settingsManager;

        Key = key;
        IsDismissed = settingsManager.Get<UI.Settings.AlertSettings>().IsDismissed(key);
    }

    public void DismissAlert()
    {
        IsDismissed = true;

        _settingsManager?.Update<UI.Settings.AlertSettings>(alertSettings => alertSettings with
        {
            AlertStatus = alertSettings.AlertStatus.SetItem(Key, true),
        });
    }
}
