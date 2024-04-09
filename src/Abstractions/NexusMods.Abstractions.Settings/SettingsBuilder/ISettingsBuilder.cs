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

file record MySettings : ISettings
{
    public string Name { get; init; } = "Initial Value";

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder.AddToUI<MySettings>(uiBuilder => uiBuilder
            .AddToSection(SectionId.DefaultValue)
            .AddPropertyToUI(x => x.Name, propertyBuilder => propertyBuilder
                .WithDisplayName("Cool Name")
                .WithDescription("This is a very cool name that you can change!")
                .WithValidation(ValidateName)
                .RequiresRestart("Changing this very cool name requires a restart!")
            )
        );
    }

    private static ValidationResult ValidateName(string name)
    {
        const int minLength = 5;
        const int maxLength = 50;

        if (string.IsNullOrWhiteSpace(name))
            return ValidationResult.CreateFailed("Name cannot be empty!");

        if (name.Length < minLength)
            return ValidationResult.CreateFailed($"Name must be at least {minLength} characters long!");

        if (name.Length > maxLength)
            return ValidationResult.CreateFailed($"Name must be less than {minLength} characters long!");

        return ValidationResult.CreateSuccessful();
    }
}
