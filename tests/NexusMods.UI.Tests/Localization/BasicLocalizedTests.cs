using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using FluentAssertions;
using NexusMods.App.UI.Localization;

namespace NexusMods.UI.Tests.Localization;

public class BasicLocalizedTests
{
    private readonly IServiceProvider _provider;
    public BasicLocalizedTests(IServiceProvider provider)
    {
        _provider = provider;
    }

    [Fact]
    public void ProvideValue_NewStringIsReturnedInCodeBehind()
    {
        var localized = new LocalizedExtension("MyGames");

        var originalString = GetStringFromBinding((CompiledBindingExtension) localized.ProvideValue(_provider));
        Localizer.Instance.LoadLanguage("pl");
        var newString = GetStringFromBinding((CompiledBindingExtension) localized.ProvideValue(_provider));

        newString.Should().NotBe(originalString);
    }

    private string GetStringFromBinding(CompiledBindingExtension binding)
    {
        // Good info: https://github.com/AvaloniaUI/Avalonia/discussions/11401
        var dummy = new TextBlock();
        var instanced = binding.Initiate(dummy, TextBlock.TextProperty);

        // If Observable is null, then it probably was OneWayToSource binding,
        // and it's not possible to read.
        var result = "";
        if (instanced?.Source is { } observable)
        {
            observable.Subscribe(s =>
            {
                result = (string)s!;
            }).Dispose();
        }

        return result;
    }
}
