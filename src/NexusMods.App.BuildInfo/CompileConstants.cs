using JetBrains.Annotations;

namespace NexusMods.Common;

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
#elif INSTALLATION_METHOD_FLATPAK
        InstallationMethod.Flatpak;
#elif INSTALLATION_METHOD_INNO_SETUP
        InstallationMethod.InnoSetup;
#else
        InstallationMethod.Manually;
#endif

    /// <summary>
    /// True if the app is running with compile settings
    /// </summary>
#if DEBUG
    public static readonly bool IsDebug = true;
#else
    public static readonly bool IsDebug = false;
#endif
}

/// <summary>
/// Method used to install the app.
/// </summary>
[PublicAPI]
public enum InstallationMethod
{
    /// <summary>
    /// Manual.
    /// </summary>
    Manually = 0,

    /// <summary>
    /// Via the archive.
    /// </summary>
    Archive,

    /// <summary>
    /// Via the AppImage.
    /// </summary>
    AppImage,

    /// <summary>
    /// Via the Flatpak.
    /// </summary>
    Flatpak,

    /// <summary>
    /// Via the Inno Setup.
    /// </summary>
    InnoSetup
}
