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
            - This will give you the correct icon size, as padding etc. is preserved.

    How to Import SVG:
    
        Exporting from Figma may give you an SVG like
        
        ```xml
        <svg width="25" height="25" viewBox="0 0 25 25" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path fill-rule="evenodd" clip-rule="evenodd" d="M12.1979 15.4946L6.68644 11.2096L5.47498 12.1518L12.2053 17.3866L18.9357 12.1518L17.7167 11.2021L12.1979 15.4946ZM12.1979 19.2336L6.68644 14.9486L5.47498 15.8908L12.2053 21.1255L18.9357 15.8908L17.7167 14.9411L12.1979 19.2336ZM12.2053 13.5951L17.7093 9.31006L18.9357 8.36033L12.2053 3.12561L5.47498 8.36033L6.69392 9.31006L12.2053 13.5951Z" fill="#F4F4F5"/>
        </svg>
        ```
        
        You have to remove the 'fill' info. So remove `fill="none"`, `fill="#F4F4F5"`
        and `fill-rule="evenodd"`. This will allow recolouring of the icon.
    
        ```xml
        <svg width="25" height="25" viewBox="0 0 25 25" xmlns="http://www.w3.org/2000/svg">
        <path clip-rule="evenodd" d="M12.1979 15.4946L6.68644 11.2096L5.47498 12.1518L12.2053 17.3866L18.9357 12.1518L17.7167 11.2021L12.1979 15.4946ZM12.1979 19.2336L6.68644 14.9486L5.47498 15.8908L12.2053 21.1255L18.9357 15.8908L17.7167 14.9411L12.1979 19.2336ZM12.2053 13.5951L17.7093 9.31006L18.9357 8.36033L12.2053 3.12561L5.47498 8.36033L6.69392 9.31006L12.2053 13.5951Z"/>
        </svg>
        ```
*/

public static class IconValues
{
#region Action
    // https://pictogrammers.com/library/mdi/icon/code-tags/
    public static readonly IconValue Code = new ProjektankerIcon("mdi-code-tags");

    // https://pictogrammers.com/library/mdi/icon/check-circle/
    public static readonly IconValue CheckCircle = new ProjektankerIcon("mdi-check-circle");

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

    // https://pictogrammers.com/library/mdi/icon/poll
    public static readonly IconValue BarChart = new ProjektankerIcon("mdi-poll");

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

    // https://pictogrammers.com/library/mdi/icon/monitor/
    public static readonly IconValue Desktop = new ProjektankerIcon("mdi-monitor");

    // https://pictogrammers.com/library/mdi/icon/gamepad-square/
    public static readonly IconValue Game = new ProjektankerIcon("mdi-gamepad-square");

#endregion

#region Image

    // https://pictogrammers.com/library/mdi/icon/image/
    public static readonly IconValue Image = new ProjektankerIcon("mdi-image");

    // https://pictogrammers.com/library/mdi/icon/tune/
    public static readonly IconValue Tune = new ProjektankerIcon("mdi-tune");
    
    // https://pictogrammers.com/library/mdi/icon/palette/
    public static readonly IconValue ColorLens = new ProjektankerIcon("mdi-palette");
    
    

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
    
    // https://pictogrammers.com/library/mdi/icon/chevron-down/
    public static readonly IconValue ChevronDown = new ProjektankerIcon("mdi-chevron-down");
    
    // https://pictogrammers.com/library/mdi/icon/chevron-up/
    public static readonly IconValue ChevronUp = new ProjektankerIcon("mdi-chevron-up");

    // https://pictogrammers.com/library/mdi/icon/close/
    public static readonly IconValue Close = new ProjektankerIcon("mdi-close");
    
    // https://pictogrammers.com/library/mdi/icon/window-minimize/
    public static readonly IconValue WindowMinimize = new ProjektankerIcon("mdi-window-minimize");
    
    // https://pictogrammers.com/library/mdi/icon/window-maximize/
    public static readonly IconValue WindowMaximize = new ProjektankerIcon("mdi-window-maximize");
    
    // https://pictogrammers.com/library/mdi/icon/refresh/
    public static readonly IconValue Refresh = new ProjektankerIcon("mdi-refresh");

#endregion
    
#region Notification
    
    // https://pictogrammers.com/library/mdi/icon/sync/
    public static readonly IconValue Sync = new ProjektankerIcon("mdi-sync");
    
#endregion

#region Social

    // https://pictogrammers.com/library/mdi/icon/school
    public static readonly IconValue School = new ProjektankerIcon("mdi-school");

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
    public static readonly IconValue Mods = new AvaloniaSvg("avares://NexusMods.App.UI/Assets/Icons/mods.svg");

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Collections = new AvaloniaSvg("avares://NexusMods.App.UI/Assets/Icons/collections.svg");
    
    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue ListFilled = new AvaloniaSvg("avares://NexusMods.App.UI/Assets/Icons/list_filled_24px.svg");

    // https://pictogrammers.com/library/mdi/icon/progress-download/
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

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue ShieldHalfFull = new AvaloniaPathIcon(
        Geometry.Parse(
            "M21.4751 11.1256C21.4751 16.6756 17.6351 21.8656 12.4751 23.1256C7.3151 21.8656 3.4751 16.6756 3.4751 11.1256V5.12561L12.4751 1.12561L21.4751 5.12561V11.1256ZM12.4751 21.1256C16.2251 20.1256 19.4751 15.6656 19.4751 11.3456V6.42561L12.4751 3.30561V21.1256Z"
        )
    );

#endregion
}
