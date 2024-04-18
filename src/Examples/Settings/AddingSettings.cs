using NexusMods.Abstractions.Settings;
// ReSharper disable All

namespace Examples.Settings;

// Create a new record class for your settings. The class needs a default constructor!
file record MySettings : ISettings
{
    // Make sure to set a default value for all properties.
    public string Name { get; init; } = "Initial Value";

    // This is a static method from the interface that you have to implement.
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // You can use the ISettingsBuilder to expose the properties defined in
        // this class in the UI.
        return settingsBuilder.AddToUI<MySettings>(uiBuilder => uiBuilder
             // You have to configure every property that you want to add to the UI individually.
            .AddPropertyToUI(x => x.Name, propertyBuilder => propertyBuilder
                // TODO: Sections
                .AddToSection(SectionId.DefaultValue)
                .WithDisplayName("Cool Name")
                .WithDescription("This is a very cool name that you can change!")
                // Optionally, you can have validation to prevent users from inputting weird stuff:
                .WithValidation(ValidateName)
                // Optionally, you can mark a property to require a restart if it changes.
                // Example for this is changing the language or other major changes that
                // can't be applied on the fly.
                // You can define a custom message:
                .RequiresRestart("Changing this very cool name requires a restart!")
                // Or default to a generic message:
                .RequiresRestart()
            )
        );
    }

    private static ValidationResult ValidateName(string name)
    {
        const int minLength = 5;
        const int maxLength = 50;

        // Validation is done by simply returning a ValidationResult.
        // Validation can either fail or succeed. If it fails, provide a reason:
        if (string.IsNullOrWhiteSpace(name))
            return ValidationResult.CreateFailed("Name cannot be empty!");

        if (name.Length < minLength)
            return ValidationResult.CreateFailed($"Name must be at least {minLength} characters long!");

        if (name.Length > maxLength)
            return ValidationResult.CreateFailed($"Name must be less than {minLength} characters long!");

        return ValidationResult.CreateSuccessful();
    }
}
