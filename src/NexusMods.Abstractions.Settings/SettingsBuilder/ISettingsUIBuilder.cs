using System.Linq.Expressions;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Configuration builder for <typeparamref name="TSettings"/> that gets exposed in the UI.
/// </summary>
[PublicAPI]
public interface ISettingsUIBuilder<TSettings>
    where TSettings : class, ISettings, new()
{
    /// <summary>
    /// Adds the selected property to the UI.
    /// </summary>
    ISettingsUIBuilder<TSettings> AddPropertyToUI<TProperty>(
        Expression<Func<TSettings, TProperty>> selectProperty,
        Func<IPropertyUIBuilder<TSettings, TProperty>, IPropertyUIBuilder<TSettings, TProperty>.IFinishedStep> configureProperty
    ) where TProperty : notnull;
}
