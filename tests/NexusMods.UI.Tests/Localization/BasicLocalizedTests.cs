using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using FluentAssertions;
using NexusMods.App.UI.Localization;

namespace NexusMods.UI.Tests.Localization;

[Collection("Localization")]
public class BasicLocalizedTests
{
    private readonly IServiceProvider _provider;
    public BasicLocalizedTests(IServiceProvider provider)
    {
        _provider = provider;
    }

    [Fact]
    [Trait("FlakeyTest", "True")]
    public void ProvideValue_NewStringIsReturnedInCodeBehind()
    {
        var localized = new LocalizedExtension("MyGames");
        var originalString = GetStringFromBinding((CompiledBindingExtension) localized.ProvideValue(_provider));
        Localizer.Instance.LoadLanguage("pl");
        var newString = GetStringFromBinding((CompiledBindingExtension) localized.ProvideValue(_provider));

        newString.Should().NotBe(originalString);

        // Restore the language after the test.
        Localizer.Instance.LoadLanguage("en");
    }

    [Fact]
    [Trait("FlakeyTest", "True")]
    public void ProvideValue_NewStringIsReturnedViaObservable_WhenLanguageIsChanged()
    {
        var localized = new LocalizedExtension("MyGames");
        var binding = (CompiledBindingExtension)localized.ProvideValue(_provider);

        var dummy = new TextBlock();
        var instanced = binding.Initiate(dummy, TextBlock.TextProperty);
        var result = "";
        IDisposable disposable = null!;
        if (instanced?.Source is { } observable)
        {
            disposable = observable.Subscribe(s =>
            {
                result = (string)s!;
            });
        }
        else
        {
            Assert.Fail("Observable is null");
        }

        // Ensure we have default value.
        result.Should().NotBeNullOrEmpty();
        var lastValue = result;

        // Change the locale, this should emit a new value in the observable.
        Localizer.Instance.LoadLanguage("pl");
        var currentValue = result;

        lastValue.Should().NotBe(currentValue);
        disposable.Dispose();

        // Restore the language after the test.
        Localizer.Instance.LoadLanguage("en");
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
