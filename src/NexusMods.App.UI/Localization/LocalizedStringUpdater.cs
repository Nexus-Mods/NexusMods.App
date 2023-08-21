namespace NexusMods.App.UI.Localization;

/// <summary>
/// Utility class for updating localized strings.
/// </summary>
public class LocalizedStringUpdater : IDisposable
{
    private readonly Action _refreshString;

    /// <summary>
    /// Creates an instance of the utility class to update localized strings.
    /// </summary>
    /// <param name="refreshString">
    ///     The callback that will be fired for every new string.
    ///     This callback is also fired during initialization of this constructor.
    /// </param>
    public LocalizedStringUpdater(Action refreshString)
    {
        _refreshString = refreshString;
        refreshString();
        Localizer.Instance.LocaleChanged += InstanceOnLocaleChanged;
    }

    private void InstanceOnLocaleChanged() => _refreshString();

    /// <inheritdoc />
    public void Dispose() => Localizer.Instance.LocaleChanged -= InstanceOnLocaleChanged;
}
