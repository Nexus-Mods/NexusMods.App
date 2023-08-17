using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

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
        // Build binding for '$"[{Key}]"'.

        // Note: CompiledBindingExtension would be nicer to use, for performance reasons; but the things
        // needed to manually create one, are internal :(

        var x = new CompiledBindingPathBuilder();
        x = x.SetRawSource(Localizer.Instance);
        x = x.Property(
            new ClrPropertyInfo(
                "Item",
                obj => ((Localizer)obj)[Key],
                null,
                typeof(string)),
            PropertyInfoAccessorFactory.CreateInpcPropertyAccessor);

        var binding = new CompiledBindingExtension(x.Build())
        {
            Mode = BindingMode.OneWay,
            Source = Localizer.Instance,
        };

        return binding.ProvideValue(serviceProvider);
    }
}
