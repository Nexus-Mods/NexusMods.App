using Avalonia;
using Avalonia.Media;
using NexusMods.Icons.SimpleVector;

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
        - Create a SimpleVectorIconImage based on the contents of an SVG 
            - This will give you the correct icon size, as padding etc. is preserved.
            - We can't use the raw SVGs as they don't support recolouring.
        - If you explicitly don't want recolouring for brand purposes, import an SVG like
            - AvaloniaSvg("avares://NexusMods.App.UI/Assets/Icons/disk_20px.svg");

    How to Import SVG:
    
        Exporting from Figma may give you an SVG like
        
        ```xml
        <svg width="25" height="25" viewBox="0 0 25 25" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path fill-rule="evenodd" clip-rule="evenodd" d="M12.46 17.9912L18.1722 13.5441L19.445 12.5584L12.46 7.12561L5.47498 12.5584L6.74004 13.5441L12.46 17.9912Z" fill="#F4F4F5"/>
        </svg>
        ```
        
        You have to extract the `d` attribute from the `path` tag, and the `viewBox` attribute from the `svg` tag.
        
        Then create a `SimpleVectorIconImage` with the `d` attribute as the `pathData` and the `viewBox` attribute as the `viewBox`.
        
        ```csharp
        public static readonly IconValue Mods = new SimpleVectorIconImage(
            "M12.46 17.9912L18.1722 13.5441L19.445 12.5584L12.46 7.12561L5.47498 12.5584L6.74004 13.5441L12.46 17.9912Z",
            new Rect(0, 0, 25, 25)
        );
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
    public static readonly IconValue Mods = new SimpleVectorIcon(new SimpleVectorIconImage(
        "M12.46 17.9912L18.1722 13.5441L19.445 12.5584L12.46 7.12561L5.47498 12.5584L6.74004 13.5441L12.46 17.9912Z",
        new Rect(0, 0, 25, 25)
        ));

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Collections = new SimpleVectorIcon(new SimpleVectorIconImage(
        "M12.1979 15.4946L6.68644 11.2096L5.47498 12.1518L12.2053 17.3866L18.9357 12.1518L17.7167 11.2021L12.1979 15.4946ZM12.1979 19.2336L6.68644 14.9486L5.47498 15.8908L12.2053 21.1255L18.9357 15.8908L17.7167 14.9411L12.1979 19.2336ZM12.2053 13.5951L17.7093 9.31006L18.9357 8.36033L12.2053 3.12561L5.47498 8.36033L6.69392 9.31006L12.2053 13.5951Z",
        new Rect(0, 0, 25, 25)
    ));
    
    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue ListFilled = new SimpleVectorIcon(new SimpleVectorIconImage(
        "M9.39429 18.8744H20.3943V16.1994H9.39429V18.8744ZM4.39429 9.54939H7.39429V6.87439H4.39429V9.54939ZM4.39429 14.2244H7.39429V11.5494H4.39429V14.2244ZM4.39429 18.8744H7.39429V16.1994H4.39429V18.8744ZM9.39429 14.2244H20.3943V11.5494H9.39429V14.2244ZM9.39429 9.54939H20.3943V6.87439H9.39429V9.54939ZM4.39429 20.8744C3.84429 20.8744 3.37345 20.6786 2.98179 20.2869C2.59012 19.8952 2.39429 19.4244 2.39429 18.8744V6.87439C2.39429 6.32439 2.59012 5.85356 2.98179 5.46189C3.37345 5.07022 3.84429 4.87439 4.39429 4.87439H20.3943C20.9443 4.87439 21.4151 5.07022 21.8068 5.46189C22.1985 5.85356 22.3943 6.32439 22.3943 6.87439V18.8744C22.3943 19.4244 22.1985 19.8952 21.8068 20.2869C21.4151 20.6786 20.9443 20.8744 20.3943 20.8744H4.39429Z",
        new Rect(0, 0, 25, 25)
    ));
    
    // https://pictogrammers.com/library/mdi/icon/progress-download/
    public static readonly IconValue Downloading = new ProjektankerIcon("mdi-progress-download");

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue ModLibrary = new SimpleVectorIcon(new SimpleVectorIconImage(
        "M18.3721 4.87439H6.40772C6.40772 4.87439 6.1358 2.87439 8.03922 2.87439H17.2844C18.644 2.87439 18.3721 4.87439 18.3721 4.87439ZM22.3943 20.5411V11.2077C22.3943 9.92439 21.4943 8.87439 20.3943 8.87439H4.39429C3.29429 8.87439 2.39429 9.92439 2.39429 11.2077V20.5411C2.39429 21.8244 3.29429 22.8744 4.39429 22.8744H20.3943C21.4943 22.8744 22.3943 21.8244 22.3943 20.5411ZM4.41219 7.87439H20.3647C20.3647 7.87439 20.7272 5.87439 18.9145 5.87439H6.58753C4.04963 5.87439 4.41219 7.87439 4.41219 7.87439ZM12.3943 11.8744L18.3943 15.8805L12.3943 19.8744L6.39429 15.8805L12.3943 11.8744Z",
        new Rect(0, 0, 25, 25)
    ));

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Discord = new SimpleVectorIcon(new SimpleVectorIconImage(
        "M19.4058 5.38929C18.1311 4.80439 16.7641 4.37346 15.3349 4.12665C15.3089 4.12188 15.2829 4.13379 15.2695 4.15759C15.0937 4.47027 14.8989 4.87819 14.7626 5.19881C13.2253 4.96867 11.696 4.96867 10.1902 5.19881C10.0538 4.87106 9.85205 4.47027 9.67546 4.15759C9.66205 4.13458 9.63605 4.12268 9.61002 4.12665C8.18157 4.37267 6.81461 4.8036 5.53909 5.38929C5.52805 5.39405 5.51858 5.40199 5.5123 5.4123C2.91947 9.28593 2.20918 13.0644 2.55763 16.7959C2.5592 16.8142 2.56945 16.8317 2.58364 16.8428C4.29432 18.099 5.9514 18.8617 7.57771 19.3672C7.60374 19.3752 7.63131 19.3657 7.64788 19.3442C8.03258 18.8189 8.37551 18.2649 8.66954 17.6824C8.68689 17.6483 8.67033 17.6078 8.63486 17.5943C8.09092 17.388 7.57298 17.1364 7.07475 16.8507C7.03534 16.8277 7.03219 16.7713 7.06844 16.7443C7.17329 16.6658 7.27816 16.584 7.37827 16.5015C7.39638 16.4864 7.42162 16.4832 7.44292 16.4928C10.716 17.9871 14.2596 17.9871 17.4941 16.4928C17.5154 16.4824 17.5406 16.4856 17.5595 16.5007C17.6597 16.5832 17.7645 16.6658 17.8702 16.7443C17.9064 16.7713 17.904 16.8277 17.8646 16.8507C17.3664 17.1419 16.8485 17.388 16.3037 17.5935C16.2683 17.607 16.2525 17.6483 16.2698 17.6824C16.5702 18.2641 16.9131 18.818 17.2907 19.3434C17.3065 19.3657 17.3349 19.3752 17.3609 19.3672C18.9951 18.8617 20.6522 18.099 22.3628 16.8428C22.3778 16.8317 22.3873 16.815 22.3889 16.7967C22.8059 12.4826 21.6904 8.73517 19.4318 5.41309C19.4263 5.40199 19.4169 5.39405 19.4058 5.38929ZM9.15833 14.5238C8.17289 14.5238 7.36092 13.6191 7.36092 12.508C7.36092 11.3969 8.15715 10.4922 9.15833 10.4922C10.1674 10.4922 10.9715 11.4049 10.9557 12.508C10.9557 13.6191 10.1595 14.5238 9.15833 14.5238ZM15.8039 14.5238C14.8185 14.5238 14.0066 13.6191 14.0066 12.508C14.0066 11.3969 14.8028 10.4922 15.8039 10.4922C16.813 10.4922 17.6171 11.4049 17.6013 12.508C17.6013 13.6191 16.813 14.5238 15.8039 14.5238Z",
        new Rect(0, 0, 25, 25)
    ));

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Forum = new SimpleVectorIcon(new SimpleVectorIconImage(
        "M15.4751 4.12561V11.1256H5.6451L4.4751 12.2956V4.12561H15.4751ZM16.4751 2.12561H3.4751C2.9251 2.12561 2.4751 2.57561 2.4751 3.12561V17.1256L6.4751 13.1256H16.4751C17.0251 13.1256 17.4751 12.6756 17.4751 12.1256V3.12561C17.4751 2.57561 17.0251 2.12561 16.4751 2.12561ZM21.4751 6.12561H19.4751V15.1256H6.4751V17.1256C6.4751 17.6756 6.9251 18.1256 7.4751 18.1256H18.4751L22.4751 22.1256V7.12561C22.4751 6.57561 22.0251 6.12561 21.4751 6.12561Z",
        new Rect(0, 0, 25, 25)
    ));
    
    // Custom Icon from Figma. The source of this icon is currently unknown.
    // Need to ask. - Sewer
    public static readonly IconValue HardDrive = new SimpleVectorIcon(new SimpleVectorIconImage(
        "M3.33317 14.1665H16.6665V9.1665H3.33317V14.1665ZM14.1665 12.9165C14.5137 12.9165 14.8089 12.795 15.0519 12.5519C15.295 12.3089 15.4165 12.0137 15.4165 11.6665C15.4165 11.3193 15.295 11.0241 15.0519 10.7811C14.8089 10.538 14.5137 10.4165 14.1665 10.4165C13.8193 10.4165 13.5241 10.538 13.2811 10.7811C13.038 11.0241 12.9165 11.3193 12.9165 11.6665C12.9165 12.0137 13.038 12.3089 13.2811 12.5519C13.5241 12.795 13.8193 12.9165 14.1665 12.9165ZM18.3332 7.49984H15.979L14.3123 5.83317H5.68734L4.02067 7.49984H1.6665L4.52067 4.64567C4.67345 4.49289 4.85053 4.37484 5.05192 4.2915C5.25331 4.20817 5.46512 4.1665 5.68734 4.1665H14.3123C14.5346 4.1665 14.7464 4.20817 14.9478 4.2915C15.1491 4.37484 15.3262 4.49289 15.479 4.64567L18.3332 7.49984ZM3.33317 15.8332C2.87484 15.8332 2.48248 15.67 2.15609 15.3436C1.8297 15.0172 1.6665 14.6248 1.6665 14.1665V7.49984H18.3332V14.1665C18.3332 14.6248 18.17 15.0172 17.8436 15.3436C17.5172 15.67 17.1248 15.8332 16.6665 15.8332H3.33317Z",
        new Rect(0, 0, 20, 20)
    ));

    // The Black and White Nexus 'Developer' Logo.
    // This is the variation of the Nexus logo used in the App, and on the Discord.
    public static readonly IconValue Nexus = new AvaloniaSvg("avares://NexusMods.App.UI/Assets/nexus-logo-white.svg");

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Stethoscope = new SimpleVectorIcon(new SimpleVectorIconImage(
        "M13.8943 22.8744C12.0943 22.8744 10.561 22.2411 9.29429 20.9744C8.02762 19.7077 7.39429 18.1744 7.39429 16.3744V15.7994C5.96095 15.5661 4.76929 14.8952 3.81929 13.7869C2.86929 12.6786 2.39429 11.3744 2.39429 9.87439V3.87439H5.39429V2.87439H7.39429V6.87439H5.39429V5.87439H4.39429V9.87439C4.39429 10.9744 4.78595 11.9161 5.56929 12.6994C6.35262 13.4827 7.29429 13.8744 8.39429 13.8744C9.49429 13.8744 10.436 13.4827 11.2193 12.6994C12.0026 11.9161 12.3943 10.9744 12.3943 9.87439V5.87439H11.3943V6.87439H9.39429V2.87439H11.3943V3.87439H14.3943V9.87439C14.3943 11.3744 13.9193 12.6786 12.9693 13.7869C12.0193 14.8952 10.8276 15.5661 9.39429 15.7994V16.3744C9.39429 17.6244 9.83179 18.6869 10.7068 19.5619C11.5818 20.4369 12.6443 20.8744 13.8943 20.8744C15.1443 20.8744 16.2068 20.4369 17.0818 19.5619C17.9568 18.6869 18.3943 17.6244 18.3943 16.3744V14.6994C17.811 14.4994 17.3318 14.1411 16.9568 13.6244C16.5818 13.1077 16.3943 12.5244 16.3943 11.8744C16.3943 11.0411 16.686 10.3327 17.2693 9.74939C17.8526 9.16606 18.561 8.87439 19.3943 8.87439C20.2276 8.87439 20.936 9.16606 21.5193 9.74939C22.1026 10.3327 22.3943 11.0411 22.3943 11.8744C22.3943 12.5244 22.2068 13.1077 21.8318 13.6244C21.4568 14.1411 20.9776 14.4994 20.3943 14.6994V16.3744C20.3943 18.1744 19.761 19.7077 18.4943 20.9744C17.2276 22.2411 15.6943 22.8744 13.8943 22.8744ZM19.3943 12.8744C19.6776 12.8744 19.9151 12.7786 20.1068 12.5869C20.2985 12.3952 20.3943 12.1577 20.3943 11.8744C20.3943 11.5911 20.2985 11.3536 20.1068 11.1619C19.9151 10.9702 19.6776 10.8744 19.3943 10.8744C19.111 10.8744 18.8735 10.9702 18.6818 11.1619C18.4901 11.3536 18.3943 11.5911 18.3943 11.8744C18.3943 12.1577 18.4901 12.3952 18.6818 12.5869C18.8735 12.7786 19.111 12.8744 19.3943 12.8744Z",
        new Rect(0, 0, 25, 25)
    ));

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue ShieldHalfFull = new SimpleVectorIcon(new SimpleVectorIconImage(
        "M21.4751 11.1256C21.4751 16.6756 17.6351 21.8656 12.4751 23.1256C7.3151 21.8656 3.4751 16.6756 3.4751 11.1256V5.12561L12.4751 1.12561L21.4751 5.12561V11.1256ZM12.4751 21.1256C16.2251 20.1256 19.4751 15.6656 19.4751 11.3456V6.42561L12.4751 3.30561V21.1256Z",
        new Rect(0, 0, 25, 25)
    ));
#endregion
}
