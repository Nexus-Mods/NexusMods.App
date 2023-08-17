using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace NexusMods.App.UI.Localization;

/// <summary>
/// A XAML Markup Extension that provides localized strings.
/// It can be used in XAML like this:
///     `<TextBlock Text="{Localized HelloWorld}"/>`
///
/// Where `HelloWorld` is the key of the string to be localized inside the `.resx` file.
/// </summary>
public class LocalizedExtension : MarkupExtension
{
    /// <summary>
    /// The key in the .resx file to be localized.
    /// </summary>
    public string Key { get; set; }

    public LocalizedExtension(string key) => Key = key;

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // Tip: This binds the [] method of the Localizer class.

        // Note: CompiledBindingExtension would be nicer to use, for performance reasons; but the things
        // needed to manually create one, are internal :(
        var binding = new ReflectionBindingExtension($"[{Key}]")
        {
            Mode = BindingMode.OneWay,
            Source = Localizer.Instance,
        };

        return binding.ProvideValue(serviceProvider);
    }
}
