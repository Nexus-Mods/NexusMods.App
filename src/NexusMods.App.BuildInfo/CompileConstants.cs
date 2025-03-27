using JetBrains.Annotations;

namespace NexusMods.App.BuildInfo;

/// <summary>
/// Constants supplied during compile time.
/// </summary>
[PublicAPI]
public static class CompileConstants
{
    /// <summary>
    /// The method used to install the app. This is set at compile time.
    /// </summary>
    public static readonly InstallationMethod InstallationMethod =
#if INSTALLATION_METHOD_ARCHIVE
        InstallationMethod.Archive;
#elif INSTALLATION_METHOD_APPIMAGE
        InstallationMethod.AppImage;
#elif INSTALLATION_METHOD_PACKAGE_MANAGER
        InstallationMethod.PackageManager;
#elif INSTALLATION_METHOD_INNO_SETUP
        InstallationMethod.InnoSetup;
#elif INSTALLATION_METHOD_FLATPAK
        InstallationMethod.Flatpak;
#else
        InstallationMethod.Manually;
#endif

    /// <summary>
    /// True if the application is running in debug mode.
    /// </summary>
    public static readonly bool IsDebug =
#if DEBUG
    true;
#else
    false;
#endif
}

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
