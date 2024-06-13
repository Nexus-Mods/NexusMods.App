using Avalonia.Media;

namespace NexusMods.Icons;

// https://www.figma.com/file/8pjtQeNggvVi7RWoLNGV80/%F0%9F%A7%B0-Nexus-Mods-Design-System?type=design&node-id=130-463

/*
    Important Notes!! - Sewer
    
    What not to do:
    
        - Paste Raw Coordinates from SVG in Figma
            - This will get you wrong icon sizes, as padding will be excluded.
            
    What to do:
    
        - Use Projektanker Icon if possible https://pictogrammers.com/library/mdi/icon/code-tags/
            - Projectanker Icons are the raw SVGs, so it's okay ^-^
        - Export SVG from Figma if icon is custom.
            - This will give you the correct icon size, as padding is preserved.
*/

public static class IconValues
{
#region Action
    // https://pictogrammers.com/library/mdi/icon/code-tags/
    public static readonly IconValue Code = new ProjektankerIcon("mdi-code-tags");

    // https://pictogrammers.com/library/mdi/icon/delete-outline/
    public static readonly IconValue DeleteOutline = new ProjektankerIcon("mdi-delete-outline");

    // https://pictogrammers.com/library/mdi/icon/delete-forever/
    public static readonly IconValue DeleteForever = new ProjektankerIcon("mdi-delete-forever");

    // https://pictogrammers.com/library/mdi/icon/file-document/
    // This is mislabeled on Figma and some places as 'description'
    public static readonly IconValue Description = new ProjektankerIcon("mdi-file-document");

    // https://pictogrammers.com/library/mdi/icon/check/
    public static readonly IconValue Done = new ProjektankerIcon("mdi-check");

    // https://pictogrammers.com/library/mdi/icon/help-circle/
    public static readonly IconValue Help = new ProjektankerIcon("mdi-help-circle");

    // https://pictogrammers.com/library/mdi/icon/help-circle-outline/
    public static readonly IconValue HelpOutline = new ProjektankerIcon("mdi-help-circle-outline");

    // https://pictogrammers.com/library/mdi/icon/history/
    public static readonly IconValue History = new ProjektankerIcon("mdi-history");

    // https://pictogrammers.com/library/mdi/icon/home/
    public static readonly IconValue Home = new ProjektankerIcon("mdi-home");

    // https://pictogrammers.com/library/mdi/icon/playlist-plus/
    public static readonly IconValue PlaylistAdd = new ProjektankerIcon("mdi-playlist-plus");
    
    // https://pictogrammers.com/library/mdi/icon/playlist-remove/
    public static readonly IconValue PlaylistRemove = new ProjektankerIcon("mdi-playlist-remove");

    // https://pictogrammers.com/library/mdi/icon/open-in-new/
    public static readonly IconValue OpenInNew = new ProjektankerIcon("mdi-open-in-new");

    // https://pictogrammers.com/library/mdi/icon/tab/
    public static readonly IconValue Tab = new ProjektankerIcon("mdi-tab");

    // https://pictogrammers.com/library/mdi/icon/magnfiy/
    public static readonly IconValue Search = new ProjektankerIcon("mdi-magnify");

    // https://pictogrammers.com/library/mdi/icon/cog/
    public static readonly IconValue Settings = new ProjektankerIcon("mdi-cog");

    // https://pictogrammers.com/library/mdi/icon/eye/
    public static readonly IconValue Visibility = new ProjektankerIcon("mdi-eye");

#endregion

#region Alert

    // https://pictogrammers.com/library/mdi/icon/alert-circle/
    public static readonly IconValue Error = new ProjektankerIcon("mdi-alert-circle");

    // https://pictogrammers.com/library/mdi/icon/alert/
    public static readonly IconValue Warning = new ProjektankerIcon("mdi-alert");

    // https://pictogrammers.com/library/mdi/icon/alert-outline/
    public static readonly IconValue WarningAmber = new ProjektankerIcon("mdi-alert-outline");

    // https://pictogrammers.com/library/mdi/icon/bell/
    public static readonly IconValue NotificationImportant = new ProjektankerIcon("mdi-bell");

#endregion

#region AV

    // https://pictogrammers.com/library/mdi/icon/pause-circle/
    public static readonly IconValue PauseCircleFilled = new ProjektankerIcon("mdi-pause-circle");

    // https://pictogrammers.com/library/mdi/icon/pause-circle-outline/
    public static readonly IconValue PauseCircleOutline = new ProjektankerIcon("mdi-pause-circle-outline");

    // https://pictogrammers.com/library/mdi/icon/play/
    public static readonly IconValue PlayArrow = new ProjektankerIcon("mdi-play");

    // https://pictogrammers.com/library/mdi/icon/play-circle/
    public static readonly IconValue PlayCircleFilled = new ProjektankerIcon("mdi-play-circle");

    // https://pictogrammers.com/library/mdi/icon/play-circle-outline/
    public static readonly IconValue PlayCircleOutline = new ProjektankerIcon("mdi-play-circle-outline");

#endregion

#region Communication

    // https://pictogrammers.com/library/mdi/icon/notification-clear-all/
    public static readonly IconValue ClearAll = new ProjektankerIcon("mdi-notification-clear-all");

    // https://pictogrammers.com/library/mdi/icon/tooltip-question/
    public static readonly IconValue LiveHelp = new ProjektankerIcon("mdi-tooltip-question");

#endregion

#region Content

    // https://pictogrammers.com/library/mdi/icon/plus/
    public static readonly IconValue Add = new ProjektankerIcon("mdi-plus");

    // https://pictogrammers.com/library/mdi/icon/plus-circle/
    public static readonly IconValue AddCircle = new ProjektankerIcon("mdi-plus-circle");

    // https://pictogrammers.com/library/mdi/icon/plus-circle-outline/
    public static readonly IconValue AddCircleOutline = new ProjektankerIcon("mdi-plus-circle-outline");

    // https://pictogrammers.com/library/mdi/icon/content-copy/
    public static readonly IconValue Copy = new ProjektankerIcon("mdi-content-copy");

    // https://pictogrammers.com/library/mdi/icon/content-paste/
    public static readonly IconValue Paste = new ProjektankerIcon("mdi-content-paste");

    // https://pictogrammers.com/library/mdi/icon/redo/
    public static readonly IconValue Redo = new ProjektankerIcon("mdi-redo");

    // https://pictogrammers.com/library/mdi/icon/minus-circle-outline/
    public static readonly IconValue RemoveCircleOutline = new ProjektankerIcon("mdi-minus-circle-outline");

    // https://pictogrammers.com/library/mdi/icon/content-save/
    public static readonly IconValue Save = new ProjektankerIcon("mdi-content-save");

    // https://pictogrammers.com/library/mdi/icon/undo/
    public static readonly IconValue Undo = new ProjektankerIcon("mdi-undo");

#endregion

#region Editor

    // https://pictogrammers.com/library/mdi/icon/drag-horizontal-variant/
    public static readonly IconValue DragHandleHorizontal = new ProjektankerIcon("mdi-drag-horizontal-variant");

    // https://pictogrammers.com/library/mdi/icon/drag-vertical-variant/
    public static readonly IconValue DragHandleVertical = new ProjektankerIcon("mdi-drag-vertical-variant");

    // https://pictogrammers.com/library/mdi/icon/file-outline/
    public static readonly IconValue File = new ProjektankerIcon("mdi-file-outline");

#endregion

#region File

    // https://pictogrammers.com/library/mdi/icon/download/
    public static readonly IconValue Download = new ProjektankerIcon("mdi-download");

    // https://pictogrammers.com/library/mdi/icon/check-underline/
    public static readonly IconValue DownloadDone = new ProjektankerIcon("mdi-check-underline");

    // https://pictogrammers.com/library/mdi/icon/folder-outline/
    public static readonly IconValue Folder = new ProjektankerIcon("mdi-folder-outline");

    // https://pictogrammers.com/library/mdi/icon/check-underline/
    public static readonly IconValue FolderOpen = new ProjektankerIcon("mdi-folder-open-outline");

    // https://pictogrammers.com/library/mdi/icon/file-edit/
    public static readonly IconValue FileEdit = new ProjektankerIcon("mdi-file-edit");

    // https://pictogrammers.com/library/mdi/icon/video-outline/
    public static readonly IconValue Video = new ProjektankerIcon("mdi-video-outline");

    // https://pictogrammers.com/library/mdi/icon/music-note/
    public static readonly IconValue MusicNote = new ProjektankerIcon("mdi-music-note");

    // https://pictogrammers.com/library/mdi/icon/file-document-outline/
    public static readonly IconValue FileDocumentOutline = new ProjektankerIcon("mdi-file-document-outline");

#endregion

#region Hardware

    // https://pictogrammers.com/library/mdi/icon/gamepad-square/
    public static readonly IconValue Game = new ProjektankerIcon("mdi-gamepad-square");

#endregion

#region Image

    // https://pictogrammers.com/library/mdi/icon/image/
    public static readonly IconValue Image = new ProjektankerIcon("mdi-image");

    // https://pictogrammers.com/library/mdi/icon/tune/
    public static readonly IconValue Tune = new ProjektankerIcon("mdi-tune");

#endregion

#region Navigation

    // https://pictogrammers.com/library/mdi/icon/arrow-left/
    public static readonly IconValue ArrowBack = new ProjektankerIcon("mdi-arrow-left");

    // https://pictogrammers.com/library/mdi/icon/arrow-right/
    public static readonly IconValue ArrowForward = new ProjektankerIcon("mdi-arrow-right");

    // https://pictogrammers.com/library/mdi/icon/menu-down/
    public static readonly IconValue ArrowDropDown = new ProjektankerIcon("mdi-menu-down");

    // https://pictogrammers.com/library/mdi/icon/menu-up/
    public static readonly IconValue ArrowDropUp = new ProjektankerIcon("mdi-menu-up");

    // https://pictogrammers.com/library/mdi/icon/chevron-left/
    public static readonly IconValue ChevronLeft = new ProjektankerIcon("mdi-chevron-left");

    // https://pictogrammers.com/library/mdi/icon/chevron-right/
    public static readonly IconValue ChevronRight = new ProjektankerIcon("mdi-chevron-right");

    // https://pictogrammers.com/library/mdi/icon/close/
    public static readonly IconValue Close = new ProjektankerIcon("mdi-close");

    // https://pictogrammers.com/library/mdi/icon/refresh/
    public static readonly IconValue Refresh = new ProjektankerIcon("mdi-refresh");

#endregion
    
#region Notification
    
    // https://pictogrammers.com/library/mdi/icon/sync/
    public static readonly IconValue Sync = new ProjektankerIcon("mdi-sync");
    
#endregion

#region Toggle

    // https://pictogrammers.com/library/mdi/icon/star/
    public static readonly IconValue Star = new ProjektankerIcon("mdi-star");

    // https://pictogrammers.com/library/mdi/icon/toggle-switch-outline/
    public static readonly IconValue ToggleOff = new ProjektankerIcon("mdi-toggle-switch-outline");

    // https://pictogrammers.com/library/mdi/icon/toggle-switch-off-outline/
    public static readonly IconValue ToggleOn = new ProjektankerIcon("mdi-toggle-switch-off-outline");

#endregion

#region Custom Icons

    // https://pictogrammers.com/library/mdi/icon/alert-octagon/
    public static readonly IconValue Alert = new ProjektankerIcon("mdi-alert-octagon");

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Mods = new AvaloniaPathIcon(
        Geometry.Parse(
            "M12.46 17.9912L18.1722 13.5441L19.445 12.5584L12.46 7.12561L5.47498 12.5584L6.74004 13.5441L12.46 17.9912Z"
        )
    );

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Collections = new AvaloniaPathIcon(
        Geometry.Parse(
            "M12.1979 15.4946L6.68644 11.2096L5.47498 12.1518L12.2053 17.3866L18.9357 12.1518L17.7167 11.2021L12.1979 15.4946ZM12.1979 19.2336L6.68644 14.9486L5.47498 15.8908L12.2053 21.1255L18.9357 15.8908L17.7167 14.9411L12.1979 19.2336ZM12.2053 13.5951L17.7093 9.31006L18.9357 8.36033L12.2053 3.12561L5.47498 8.36033L6.69392 9.31006L12.2053 13.5951Z"
        )
    );
    
    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue ListFilled = new AvaloniaPathIcon(
        Geometry.Parse(
            "M9.39429 18.8744H20.3943V16.1994H9.39429V18.8744ZM4.39429 9.54939H7.39429V6.87439H4.39429V9.54939ZM4.39429 14.2244H7.39429V11.5494H4.39429V14.2244ZM4.39429 18.8744H7.39429V16.1994H4.39429V18.8744ZM9.39429 14.2244H20.3943V11.5494H9.39429V14.2244ZM9.39429 9.54939H20.3943V6.87439H9.39429V9.54939ZM4.39429 20.8744C3.84429 20.8744 3.37345 20.6786 2.98179 20.2869C2.59012 19.8952 2.39429 19.4244 2.39429 18.8744V6.87439C2.39429 6.32439 2.59012 5.85356 2.98179 5.46189C3.37345 5.07022 3.84429 4.87439 4.39429 4.87439H20.3943C20.9443 4.87439 21.4151 5.07022 21.8068 5.46189C22.1985 5.85356 22.3943 6.32439 22.3943 6.87439V18.8744C22.3943 19.4244 22.1985 19.8952 21.8068 20.2869C21.4151 20.6786 20.9443 20.8744 20.3943 20.8744H4.39429Z"
        )
    );

    // https://pictogrammers.com/library/mdi/icon/star/
    public static readonly IconValue Downloading = new ProjektankerIcon("mdi-progress-download");

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue ModLibrary = new AvaloniaSvg("avares://NexusMods.App.UI/Assets/Icons/mod_library.svg");

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Discord = new AvaloniaSvg("avares://NexusMods.App.UI/Assets/Icons/discord.svg");

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Forum = new AvaloniaSvg("avares://NexusMods.App.UI/Assets/Icons/forum.svg");
    
    // Custom Icon from Figma. The source of this icon is currently unknown.
    // Need to ask. - Sewer
    public static readonly IconValue HardDrive = new AvaloniaSvg("avares://NexusMods.App.UI/Assets/Icons/disk_20px.svg");

    // The Black and White Nexus 'Developer' Logo.
    // This is the variation of the Nexus logo used in the App, and on the Discord.
    public static readonly IconValue Nexus = new AvaloniaSvg("avares://NexusMods.App.UI/Assets/nexus-logo-white.svg");

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Stethoscope = new AvaloniaSvg("avares://NexusMods.App.UI/Assets/Icons/stethoscope_24px.svg");

#endregion
}
