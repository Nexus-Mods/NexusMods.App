using NexusMods.Paths.Extensions;

namespace NexusMods.Paths.Utilities;

/// <summary>
/// Contains a listing of known ahead of time folders for easy access.
/// </summary>
public static class KnownFolders
{
    // TODO: We need to detect Proton/Wine for Linux and adjust these paths based on the target game(s).
    
    /// <summary>
    /// Gets the directory this program's DLL resolver uses to probe for DLLs.  
    /// This is usually the same as <see cref="CurrentDirectory"/>.  
    /// </summary>
    public static AbsolutePath EntryFolder => AppContext.BaseDirectory.ToAbsolutePath();
    
    /// <summary>
    /// Gets the current working directory of the application.
    /// </summary>
    public static AbsolutePath CurrentDirectory => Environment.CurrentDirectory.ToAbsolutePath();

    /// <summary>
    /// Path to the `Documents` folder 
    /// </summary>
    public static AbsolutePath Documents => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToAbsolutePath();

    /// <summary>
    /// Path to the 'My Games' folder; present only on Windows.
    /// </summary>
    public static AbsolutePath MyGames => Documents.Join("My Games");
    
    /// <summary>
    /// Path to the user's profile.
    /// On *nix this will usually be home/userNameHere and Windows C:\Users\userNameHere.
    /// </summary>
    public static AbsolutePath HomeFolder => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).ToAbsolutePath();
}