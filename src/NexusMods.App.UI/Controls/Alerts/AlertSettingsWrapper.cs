using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Alerts;

public sealed class AlertSettingsWrapper : ReactiveObject
{
    private readonly ISettingsManager? _settingsManager;

    public string Key { get; }

    [Reactive] public bool IsDismissed { get; private set; }

    public AlertSettingsWrapper()
    {
        _settingsManager = null;
        Key = string.Empty;
    }

    public AlertSettingsWrapper(ISettingsManager settingsManager, string key)
    {
        _settingsManager = settingsManager;
        
        Key = key;
        IsDismissed = settingsManager.Get<AlertSettings>().IsDismissed(key);
    }

    public void ToggleAlert()
    {
        IsDismissed = !IsDismissed;

        _settingsManager?.Update<AlertSettings>(alertSettings => alertSettings with
        {
            AlertStatus = alertSettings.AlertStatus.SetItem(Key, IsDismissed),
        });
    }

    public void DismissAlert()
    {
        IsDismissed = true;

        _settingsManager?.Update<AlertSettings>(alertSettings => alertSettings with
        {
            AlertStatus = alertSettings.AlertStatus.SetItem(Key, IsDismissed),
        });
    }
    
    public void ShowAlert()
    {
        IsDismissed = false;

        _settingsManager?.Update<AlertSettings>(alertSettings => alertSettings with
        {
            AlertStatus = alertSettings.AlertStatus.SetItem(Key, IsDismissed),
        });
    }
}
