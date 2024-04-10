using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Configuration builder for types that implement <see cref="ISettings"/>.
/// </summary>
[PublicAPI]
public interface ISettingsBuilder
{
    /// <summary>
    /// Configures the settings type <typeparamref name="TSettings"/> to be
    /// exposed in the UI.
    /// </summary>
    ISettingsBuilder AddToUI<TSettings>(
        Func<ISettingsUIBuilder<TSettings>, ISettingsUIBuilder<TSettings>.IFinishedStep> configureUI
    ) where TSettings : class, ISettings, new();
}
