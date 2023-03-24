using JetBrains.Annotations;

namespace NexusMods.Paths;

/// <summary>
/// Enum of known directory and file paths. This should be
/// used instead of <see cref="Environment.GetFolderPath(System.Environment.SpecialFolder)"/>.
/// </summary>
[PublicAPI]
public enum KnownPath
{
    /// <summary>
    /// The path of the base directory that the assembly
    /// resolver uses to probe for assemblies.
    /// </summary>
    /// <remarks>
    /// This is often the same as <see cref="CurrentDirectory"/>.
    /// <see cref="BaseFileSystem"/> uses <see cref="AppContext.BaseDirectory"/>
    /// to get this value.
    /// </remarks>
    EntryDirectory,

    /// <summary>
    /// The current working directory.
    /// </summary>
    /// <remarks>
    /// This is often the same as <see cref="EntryDirectory"/>.
    /// <see cref="BaseFileSystem"/> uses <see cref="Environment.CurrentDirectory"/>
    /// to get this value.
    /// </remarks>
    CurrentDirectory,

    /// <summary>
    /// The directory that serves as a common repository for application-specific
    /// data that is used by all users.
    /// <list type="table">
    ///     <listheader>
    ///         <term>OS</term>
    ///         <description>Path</description>
    ///     </listheader>
    ///     <item>
    ///         <term>Windows</term>
    ///         <description>
    ///             (CSIDL_COMMON_APPDATA) ProgramData folder <c>%ALLUSERSPROFILE%</c>
    ///             (<c>%ProgramData%"</c>, <c>%SystemDrive%\ProgramData</c>)
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Linux</term>
    ///         <description><c>/usr/share</c></description>
    ///     </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <see cref="BaseFileSystem"/> uses <see cref="Environment.GetFolderPath(System.Environment.SpecialFolder)"/>
    /// with <see cref="Environment.SpecialFolder.CommonApplicationData"/> to get this value.
    /// </remarks>
    CommonApplicationDataDirectory,

    /// <summary>
    /// The current user's temporary folder. On Linux:
    /// <list type="number">
    ///     <item>The path specified by the <c>TMPDIR</c> environment variable.</item>
    ///     <item>The path <c>/tmp</c>.</item>
    /// </list>
    /// On Windows:
    /// <list type="number">
    ///     <item>The path specified by the <c>TMP</c> environment variable.</item>
    ///     <item>The path specified by the <c>TEMP</c> environment variable.</item>
    ///     <item>The path specified by the <c>USERPROFILE</c> environment variable.</item>
    ///     <item>The Windows directory.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <see cref="BaseFileSystem"/> uses <see cref="Path.GetTempPath"/> to get this value.
    /// </remarks>
    TempDirectory,

    /// <summary>
    /// The user's profile folder.
    /// <list type="table">
    ///     <listheader>
    ///         <term>OS</term>
    ///         <description>Path</description>
    ///     </listheader>
    ///     <item>
    ///         <term>Windows</term>
    ///         <description>
    ///             (CSIDL_PROFILE) The root users profile folder <c>%USERPROFILE%</c>
    ///             (<c>%SystemDrive%\Users\%USERNAME%</c>)
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Linux</term>
    ///         <description><c>$HOME</c> or <c>/home/$USER</c></description>
    ///     </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <see cref="BaseFileSystem"/> uses <see cref="Environment.GetFolderPath(System.Environment.SpecialFolder)"/>
    /// with <see cref="Environment.SpecialFolder.UserProfile"/> to get this value.
    /// </remarks>
    HomeDirectory,

    /// <summary>
    /// The directory that serves as a common repository for application-specific data for the current roaming user. A roaming user works on more than one computer on a network.
    /// A roaming user's profile is kept on a server on the network and is loaded onto a system when the user logs on.
    /// <list type="table">
    ///     <listheader>
    ///         <term>OS</term>
    ///         <description>Path</description>
    ///     </listheader>
    ///     <item>
    ///         <term>Windows</term>
    ///         <description>
    ///             (CSIDL_APPDATA) Roaming user application data folder
    ///             <c>%APPDATA%</c> (<c>%USERPROFILE%\AppData\Roaming</c>)
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Linux</term>
    ///         <description><c>XDG_CONFIG_HOME</c> or <c>$HOME/.config</c></description>
    ///     </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <see cref="BaseFileSystem"/> uses <see cref="Environment.GetFolderPath(System.Environment.SpecialFolder)"/>
    /// with <see cref="Environment.SpecialFolder.ApplicationData"/> to get this value.
    /// </remarks>
    ApplicationDataDirectory,

    /// <summary>
    /// The directory that serves as a common repository for
    /// application-specific data that is used by the current, non-roaming user.
    /// <list type="table">
    ///     <listheader>
    ///         <term>OS</term>
    ///         <description>Path</description>
    ///     </listheader>
    ///     <item>
    ///         <term>Windows</term>
    ///         <description>
    ///             (CSIDL_LOCAL_APPDATA) Local folder
    ///             <c>%LOCALAPPDATA%</c> (<c>%USERPROFILE%\AppData\Local</c>)
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Linux</term>
    ///         <description><c>XDG_DATA_HOME</c> or <c>$HOME/.local/share</c></description>
    ///     </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <see cref="BaseFileSystem"/> uses <see cref="Environment.GetFolderPath(System.Environment.SpecialFolder)"/>
    /// with <see cref="Environment.SpecialFolder.LocalApplicationData"/> to get this value.
    /// </remarks>
    LocalApplicationDataDirectory,

    /// <summary>
    /// The My Documents folder.
    /// <list type="table">
    ///     <listheader>
    ///         <term>OS</term>
    ///         <description>Path</description>
    ///     </listheader>
    ///     <item>
    ///         <term>Windows</term>
    ///         <description>
    ///             (CSIDL_MYDOCUMENTS, CSIDL_PERSONAL) Documents (My Documents) folder
    ///             <c>%USERPROFILE%\Documents</c>
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Linux</term>
    ///         <description><c>XDG_DOCUMENTS_DIR</c> or <c>$HOME/Documents</c></description>
    ///     </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <see cref="BaseFileSystem"/> uses <see cref="Environment.GetFolderPath(System.Environment.SpecialFolder)"/>
    /// with <see cref="Environment.SpecialFolder.MyDocuments"/> to get this value.
    /// </remarks>
    MyDocumentsDirectory,

    /// <summary>
    /// The <c>My Games</c> folder, relative to <see cref="MyDocumentsDirectory"/>.
    /// </summary>
    /// <remarks>
    /// While many games like to use this folder, it is not a special folder
    /// and doesn't have a CSID. <see cref="BaseFileSystem"/> simply combines
    /// <see cref="MyDocumentsDirectory"/> with <c>My Games</c>.
    /// </remarks>
    MyGamesDirectory,
}
