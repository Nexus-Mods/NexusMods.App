using System.ComponentModel;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Markup.Xaml.MarkupExtensions;
using NexusMods.App.UI.Resources;

namespace NexusMods.App.UI.Localization;

/// <summary>
/// Utility class that provides access
/// </summary>
public class Localizer : INotifyPropertyChanged // <= INotifyPropertyChanged is required, so we can't be static
{
    /// <summary>
    /// Singleton instance of the
    /// </summary>
    public static Localizer Instance { get; set; } = new();

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
}
