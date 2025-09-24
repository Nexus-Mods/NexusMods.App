using NexusMods.Sdk.Settings;
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
        return settingsBuilder.ConfigureProperty(
            x => x.Name,
            new PropertyOptions<MySettings, string>
            {
                // TODO: Sections
                Section = SectionId.DefaultValue,
                DisplayName = "Cool Name",
                DescriptionFactory = _ => "This is a very cool name that you can change!",
                // Optionally, you can mark a property to require a restart if it changes.
                // Example for this is changing the language or other major changes that
                // can't be applied on the fly.
                RequiresRestart = true,
                // You can define a custom message to show if the value gets changed:
                RestartMessage = "Changing this very cool name requires a restart!"
            },
            // TODO: update this
            new BooleanContainerOptions()
        );
    }
}
