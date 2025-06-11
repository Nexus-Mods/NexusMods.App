using JetBrains.Annotations;

namespace NexusMods.Sdk;

/// <summary>
/// Method used to install the app.
/// </summary>
[PublicAPI]
public enum InstallationMethod
{
    /// <summary>
    /// Manual installation, or from source. This is the default value.
    /// </summary>
    Manually = 0,

    /// <summary>
    /// The App was packaged into an archive.
    /// </summary>
    Archive,

    /// <summary>
    /// The App was packaged into an AppImage.
    /// </summary>
    AppImage,

    /// <summary>
    /// The App was installed using a package manager.
    /// </summary>
    PackageManager,

    /// <summary>
    /// The App was installed using the InnoSetup.
    /// </summary>
    InnoSetup,

    /// <summary>
    /// The App was installed as a Flatpak.
    /// </summary>
    Flatpak,
}
