using System.Runtime.CompilerServices;
using NexusMods.Icons;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;
using NexusMods.Paths.Utilities.Enums;

namespace NexusMods.App.UI.Controls.Trees.Common;

public enum FileTreeNodeIconType
{
    /// <summary>
    ///     Shows a regular 'file' icon.
    ///     This is for any file that doesn't fall into the other categories.
    /// </summary>
    File,
    
    /// <summary>
    ///     Show a 'closed folder' icon.
    /// </summary>
    ClosedFolder,
    
    /// <summary>
    ///     Show an 'open folder' icon.
    /// </summary>
    OpenFolder,
    
    /// <summary>
    ///     This is a file which is an Image.
    /// </summary>
    Image,
    
    /// <summary>
    ///     This is a file which is text.
    /// </summary>
    Text,
    
    /// <summary>
    ///     This is a file which contains music or speech.
    /// </summary>
    Audio,
    
    /// <summary>
    ///     This shows movies.
    /// </summary>
    Video,
}

public static class FileTreeNodeIconTypeHelpers
{
    /// <summary>
    /// Maps an Icon Type from an extension to an Icon category used by the Nexus App UI.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FileTreeNodeIconType GetIconType(this Extension extension)
    {
        var category = extension.GetCategory();
        return GetIconType(category);
    }

    /// <summary>
    /// Maps an Icon Type from the Paths library to an Icon Category used by the Nexus App UI.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FileTreeNodeIconType GetIconType(this ExtensionCategory category) => category switch
    {
        // Images
        ExtensionCategory.Image => FileTreeNodeIconType.Image,

        // Text
        ExtensionCategory.Script => FileTreeNodeIconType.Text,
        ExtensionCategory.Text => FileTreeNodeIconType.Text,
        
        // Audio
        ExtensionCategory.Audio => FileTreeNodeIconType.Audio,
        ExtensionCategory.ArchiveOfAudio => FileTreeNodeIconType.Audio,
 
        // Video
        ExtensionCategory.Video => FileTreeNodeIconType.Video,
        
        // Files (Other)
        ExtensionCategory.Unknown => FileTreeNodeIconType.File,
        ExtensionCategory.Model => FileTreeNodeIconType.File,
        ExtensionCategory.Archive => FileTreeNodeIconType.File,
        ExtensionCategory.ArchiveOfImage => FileTreeNodeIconType.File,
        ExtensionCategory.Binary => FileTreeNodeIconType.File,
        ExtensionCategory.Library => FileTreeNodeIconType.File,
        ExtensionCategory.Database => FileTreeNodeIconType.File,
        ExtensionCategory.Executable => FileTreeNodeIconType.File,
        _ => FileTreeNodeIconType.File,
    };
    
    /// <summary>
    /// Provides the XAML class to be used with <see cref="UnifiedIcon"/> for the given icon type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("To be removed with migration to new ViewModFiles and Preview Changes")]
    public static string GetIconClass(this FileTreeNodeIconType iconType) => iconType switch
    {
        FileTreeNodeIconType.File => "File",
        FileTreeNodeIconType.ClosedFolder => "FolderOutline",
        FileTreeNodeIconType.OpenFolder => "FolderOpenOutline",
        FileTreeNodeIconType.Image => "Image",
        FileTreeNodeIconType.Text => "FileDocumentOutline",
        FileTreeNodeIconType.Audio => "MusicNote",
        FileTreeNodeIconType.Video => "VideoOutline",
        _ => ThrowArgumentOutOfRangeException(iconType),
    };
    
    /// <summary>
    /// Provides the direct <see cref="IconValues"/> for the given icon type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IconValue GetIconValue(this FileTreeNodeIconType iconType) => iconType switch
    {
        FileTreeNodeIconType.File => IconValues.File,
        FileTreeNodeIconType.ClosedFolder => IconValues.Folder,
        FileTreeNodeIconType.OpenFolder => IconValues.FolderOpen,
        FileTreeNodeIconType.Image => IconValues.Image,
        FileTreeNodeIconType.Text => IconValues.FileDocumentOutline,
        FileTreeNodeIconType.Audio => IconValues.MusicNote,
        FileTreeNodeIconType.Video => IconValues.Video,
        _ => ThrowArgumentOutOfRangeExceptionIcon(iconType),
    };

    /// <summary>
    /// Provides the XAML class to be used with <see cref="UnifiedIcon"/> for the given extension.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetIconClass(this Extension extension)
    {
        var category = extension.GetCategory();
        return GetIconClass(GetIconType(category));
    }
    
    /// <summary>
    /// Provides the XAML class to be used with <see cref="UnifiedIcon"/> for the given file name.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetIconClassFromFileName(this string fileName)
    {
        return RelativePath.FromUnsanitizedInput(fileName).Extension.GetIconClass();
    }

    private static string ThrowArgumentOutOfRangeException(FileTreeNodeIconType iconType) => throw new ArgumentOutOfRangeException(nameof(iconType), iconType, null);
    private static IconValue ThrowArgumentOutOfRangeExceptionIcon(FileTreeNodeIconType iconType) => throw new ArgumentOutOfRangeException(nameof(iconType), iconType, null);
}
