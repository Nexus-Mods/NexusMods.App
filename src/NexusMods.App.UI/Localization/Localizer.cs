using System.ComponentModel;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using NexusMods.App.UI.Resources;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Localization;

/// <summary>
/// Utility class that provides access
/// </summary>
public class Localizer : INotifyPropertyChanged // <= INotifyPropertyChanged is required, so we can't be static
{
    /// <summary>
    /// Singleton instance of the localizer.
    /// </summary>
    public static Localizer Instance { get; set; } = new();

    /// <summary>
    /// This is called when the locale of an element is changed.
    /// </summary>
    public event Action? LocaleChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    // These are 'conventional' names used by the UI framework to indicate that an indexer
    // property has been changed. When we create a binding with $"[{Key}]" syntax, the UI
    // framework binds against the this[string key] property.

    // Historically in XAML frameworks, this can be internally represented as 'Item' or 'Item[]';
    // when I had a look through the Avalonia source for creating bindings; it seems to be 'Item' for
    // array members; which I believe this would fall under. But just in case, I included both.

    private const string IndexerName = "Item";
    private const string IndexerArrayName = "Item[]";

    /// <summary>
    /// Loads a language with the specified locale, resetting the
    /// </summary>
    /// <param name="locale">The new locale to apply.</param>
    public void LoadLanguage(string locale)
    {
        Language.Culture = new CultureInfo(locale);
        Invalidate();
        LocaleChanged?.Invoke();
    }



    /// <summary>
    /// Retrieves a string associated with current language, by key.
    /// </summary>
    public string this[string key] => Language.ResourceManager.GetString(key, Language.Culture)!;

    private void Invalidate()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerName));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerArrayName));
    }

    internal static CompiledBindingExtension GetBindingForKey(string key)
    {
        // Tip: This binds the [] method of the Localizer class.
        // Build binding for '$"[{Key}]"'.
        var x = new CompiledBindingPathBuilder();
        x = x.SetRawSource(Localizer.Instance);
        x = x.Property(
            new ClrPropertyInfo(
                "Item",
                obj => ((Localizer)obj)[key],
                null,
                typeof(string)),
            PropertyInfoAccessorFactory.CreateInpcPropertyAccessor);

        var binding = new CompiledBindingExtension(x.Build())
        {
            Mode = BindingMode.OneWay,
            Source = Localizer.Instance,
        };
        return binding;
    }

    /// <summary>
    /// Creates a compiled avalonia binding for a localized string.
    /// The binding will update the string when the locale is changed.
    /// The new localized value will be retrieved using the provided <paramref name="getLocalizedString"/> function.
    /// </summary>
    /// <example>
    /// <code>
    /// [!TextBlock.TextProperty] = Localizer.CreateAvaloniaBinding(() => Language.Helpers_GenerateHeader_NAME)
    /// </code>
    /// </example>
    /// <param name="getLocalizedString">A function that returns a string localized for the current locale.</param>
    /// <returns>A compiled avalonia binding to dynamic localized string.</returns>
    public static CompiledBindingExtension CreateAvaloniaBinding(Func<string> getLocalizedString)
    {
        var x = new CompiledBindingPathBuilder();
        x = x.SetRawSource(Localizer.Instance);
        x = x.Property(
            new ClrPropertyInfo(
                "Item",
                obj => getLocalizedString(),
                null,
                typeof(string)),
            PropertyInfoAccessorFactory.CreateInpcPropertyAccessor);

        var binding = new CompiledBindingExtension(x.Build())
        {
            Mode = BindingMode.OneWay,
            Source = Localizer.Instance,
        };
        return binding;
    }

}
