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
        var binding = GetBindingForKey(Key);

        return binding.ProvideValue(serviceProvider);
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

    internal static CompiledBindingExtension GetBindingFor(Func<string> getString)
    {
        var x = new CompiledBindingPathBuilder();
        x = x.SetRawSource(Localizer.Instance);
        x = x.Property(
            new ClrPropertyInfo(
                "Item",
                obj => getString(),
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
