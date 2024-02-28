namespace NexusMods.App.UI.Pages.ViewModInfo.Types;

/// <summary>
///     Currently shown page in <see cref="ViewModInfoView"/>.
/// </summary>
public enum CurrentViewModInfoPage
{
    /// <summary>
    ///     For the file tree.
    /// </summary>
    Files,
    
    /// <summary>
    ///     Arbitrary user specific notes attached to this mod.
    /// </summary>
    Notes,
    
    /// <summary>
    ///     Views or edits the dependencies for this mod.
    /// </summary>
    Dependencies,
    
    /// <summary>
    ///     Shows other mods which have conflicting files with this mod.
    /// </summary>
    Conflicts,
    
    /// <summary>
    ///     Allows the user to assign 'tags' and 'categories' to each mod.
    /// </summary>
    Categories,
    
    /// <summary>
    ///     A mod author provided description for this mod.
    /// </summary>
    Description,
}
