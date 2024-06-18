using Avalonia.Media;

namespace NexusMods.Icons;

// https://www.figma.com/file/8pjtQeNggvVi7RWoLNGV80/%F0%9F%A7%B0-Nexus-Mods-Design-System?type=design&node-id=130-463

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

    // exported from Figma because there wasn't a fitting Material Design Icon
    public static readonly IconValue Description = new AvaloniaPathIcon(Geometry.Parse(
            "M2 0H10L16 6V18C16 19.1 15.1 20 14 20H1.99C0.89 20 0 19.1 0 18V2C0 0.9 0.9 0 2 0ZM4 16H12V14H4V16ZM12 12H4V10H12V12ZM9 1.5V7H14.5L9 1.5Z"
        )
    );

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

    // https://pictogrammers.com/library/mdi/icon/file/
    public static readonly IconValue InsertDriveFile = new ProjektankerIcon("mdi-file");

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

    // https://pictogrammers.com/library/mdi/icon/gamepad-square-outline/
    public static readonly IconValue Game = new ProjektankerIcon("mdi-gamepad-square-outline");

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
    public static readonly IconValue MonitorDiagnostics = new AvaloniaPathIcon(
        Geometry.Parse(
            "M9.07495 19.25C8.80828 19.25 8.56662 19.175 8.34995 19.025C8.13328 18.875 7.98328 18.6834 7.89995 18.45L5.47495 12.75H1.19995V11.25H6.52495L9.07495 17.275L13.75 5.60005C13.8333 5.36672 13.9833 5.17505 14.2 5.02505C14.4166 4.87505 14.6583 4.80005 14.925 4.80005C15.1916 4.80005 15.4333 4.87505 15.65 5.02505C15.8666 5.17505 16.0166 5.36672 16.1 5.60005L18.525 11.25H22.8V12.75H17.475L14.925 6.77505L10.25 18.45C10.1666 18.6834 10.0166 18.875 9.79995 19.025C9.58328 19.175 9.34162 19.25 9.07495 19.25Z"
        )
    );

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue JoystickGameFilled = new AvaloniaPathIcon(
        Geometry.Parse(
            "M8.64429 7.46191V6.76191C7.92762 6.57858 7.33179 6.19531 6.85679 5.61211C6.38179 5.02893 6.14429 4.35386 6.14429 3.58691C6.14429 2.6893 6.4618 1.92322 7.09681 1.28869C7.73183 0.654172 8.4985 0.336914 9.39681 0.336914C10.2951 0.336914 11.061 0.654172 11.6943 1.28869C12.3276 1.92322 12.6443 2.6893 12.6443 3.58691C12.6443 4.35386 12.4068 5.02893 11.9318 5.61211C11.4568 6.19531 10.861 6.57858 10.1443 6.76191V7.46191L17.6443 11.7869C17.8818 11.9231 18.0662 12.1057 18.1974 12.3348C18.3287 12.5638 18.3943 12.8145 18.3943 13.0869V15.5869C18.3943 15.8593 18.3287 16.11 18.1974 16.3391C18.0662 16.5681 17.8818 16.7507 17.6443 16.8869L10.1443 21.2119C9.90549 21.3452 9.65412 21.4119 9.39019 21.4119C9.12625 21.4119 8.87762 21.3452 8.64429 21.2119L1.14429 16.8869C0.906787 16.7507 0.722412 16.5681 0.591162 16.3391C0.459912 16.11 0.394287 15.8593 0.394287 15.5869V13.0869C0.394287 12.8145 0.459912 12.5638 0.591162 12.3348C0.722412 12.1057 0.906787 11.9231 1.14429 11.7869L8.64429 7.46191ZM8.64429 12.8369V9.18691L3.01929 12.4369L9.39429 16.1119L15.7443 12.4119L10.1443 9.18691V12.8369H8.64429Z"
        )
    );

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue DiagnosticPage = new AvaloniaPathIcon(
        Geometry.Parse(
            "M2 9.87439V6.87439C2 6.32439 2.19583 5.85356 2.5875 5.46189C2.97917 5.07022 3.45 4.87439 4 4.87439H20C20.55 4.87439 21.0208 5.07022 21.4125 5.46189C21.8042 5.85356 22 6.32439 22 6.87439V9.87439H20V6.87439H4V9.87439H2ZM4 20.8744C3.45 20.8744 2.97917 20.6786 2.5875 20.2869C2.19583 19.8952 2 19.4244 2 18.8744V15.8744H4V18.8744H20V15.8744H22V18.8744C22 19.4244 21.8042 19.8952 21.4125 20.2869C21.0208 20.6786 20.55 20.8744 20 20.8744H4ZM10 17.8744C10.1833 17.8744 10.3583 17.8286 10.525 17.7369C10.6917 17.6452 10.8167 17.5077 10.9 17.3244L14 11.1244L15.1 13.3244C15.1833 13.5077 15.3083 13.6452 15.475 13.7369C15.6417 13.8286 15.8167 13.8744 16 13.8744H22V11.8744H16.625L14.9 8.42439C14.8167 8.24106 14.6917 8.11189 14.525 8.03689C14.3583 7.96189 14.1833 7.92439 14 7.92439C13.8167 7.92439 13.6417 7.96189 13.475 8.03689C13.3083 8.11189 13.1833 8.24106 13.1 8.42439L10 14.6244L8.9 12.4244C8.81667 12.2411 8.69167 12.1036 8.525 12.0119C8.35833 11.9202 8.18333 11.8744 8 11.8744H2V13.8744H7.375L9.1 17.3244C9.18333 17.5077 9.30833 17.6452 9.475 17.7369C9.64167 17.8286 9.81667 17.8744 10 17.8744Z"
        )
    );
    
    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue ModLibrary = new AvaloniaPathIcon(
        Geometry.Parse(
            "M18.3721 4.87439H6.40772C6.40772 4.87439 6.1358 2.87439 8.03922 2.87439H17.2844C18.644 2.87439 18.3721 4.87439 18.3721 4.87439ZM22.3943 20.5411V11.2077C22.3943 9.92439 21.4943 8.87439 20.3943 8.87439H4.39429C3.29429 8.87439 2.39429 9.92439 2.39429 11.2077V20.5411C2.39429 21.8244 3.29429 22.8744 4.39429 22.8744H20.3943C21.4943 22.8744 22.3943 21.8244 22.3943 20.5411ZM4.41219 7.87439H20.3647C20.3647 7.87439 20.7272 5.87439 18.9145 5.87439H6.58753C4.04963 5.87439 4.41219 7.87439 4.41219 7.87439ZM12.3943 11.8744L18.3943 15.8805L12.3943 19.8744L6.39429 15.8805L12.3943 11.8744Z"
        )
    );

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Discord = new AvaloniaPathIcon(
        Geometry.Parse(
            "M17.4058 1.38929C16.1311 0.804386 14.7641 0.373457 13.3349 0.126646C13.3089 0.121882 13.2829 0.133786 13.2695 0.157595C13.0937 0.470274 12.8989 0.878189 12.7626 1.19881C11.2253 0.968668 9.69596 0.968668 8.19024 1.19881C8.05385 0.871062 7.85205 0.470274 7.67546 0.157595C7.66205 0.134581 7.63605 0.122677 7.61002 0.126646C6.18157 0.372668 4.81461 0.803598 3.53909 1.38929C3.52805 1.39405 3.51858 1.40199 3.5123 1.4123C0.919469 5.28593 0.209184 9.06436 0.557626 12.7959C0.559202 12.8142 0.56945 12.8317 0.583641 12.8428C2.29432 14.099 3.9514 14.8617 5.57771 15.3672C5.60374 15.3752 5.63131 15.3657 5.64788 15.3442C6.03258 14.8189 6.37551 14.2649 6.66954 13.6824C6.68689 13.6483 6.67033 13.6078 6.63486 13.5943C6.09092 13.388 5.57298 13.1364 5.07475 12.8507C5.03534 12.8277 5.03219 12.7713 5.06844 12.7443C5.17329 12.6658 5.27816 12.584 5.37827 12.5015C5.39638 12.4864 5.42162 12.4832 5.44292 12.4928C8.71605 13.9871 12.2596 13.9871 15.4941 12.4928C15.5154 12.4824 15.5406 12.4856 15.5595 12.5007C15.6597 12.5832 15.7645 12.6658 15.8702 12.7443C15.9064 12.7713 15.904 12.8277 15.8646 12.8507C15.3664 13.1419 14.8485 13.388 14.3037 13.5935C14.2683 13.607 14.2525 13.6483 14.2698 13.6824C14.5702 14.2641 14.9131 14.818 15.2907 15.3434C15.3065 15.3657 15.3349 15.3752 15.3609 15.3672C16.9951 14.8617 18.6522 14.099 20.3628 12.8428C20.3778 12.8317 20.3873 12.815 20.3889 12.7967C20.8059 8.48261 19.6904 4.73517 17.4318 1.41309C17.4263 1.40199 17.4169 1.39405 17.4058 1.38929ZM7.15833 10.5238C6.17289 10.5238 5.36092 9.61909 5.36092 8.50802C5.36092 7.39695 6.15715 6.49224 7.15833 6.49224C8.16737 6.49224 8.97148 7.40489 8.95571 8.50802C8.95571 9.61909 8.15948 10.5238 7.15833 10.5238ZM13.8039 10.5238C12.8185 10.5238 12.0066 9.61909 12.0066 8.50802C12.0066 7.39695 12.8028 6.49224 13.8039 6.49224C14.813 6.49224 15.6171 7.40489 15.6013 8.50802C15.6013 9.61909 14.813 10.5238 13.8039 10.5238Z"
        )
    );

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Forum = new AvaloniaPathIcon(
        Geometry.Parse(
            "M13.4751 2.12561V9.12561H3.6451L2.4751 10.2956V2.12561H13.4751ZM14.4751 0.12561H1.4751C0.925098 0.12561 0.475098 0.57561 0.475098 1.12561V15.1256L4.4751 11.1256H14.4751C15.0251 11.1256 15.4751 10.6756 15.4751 10.1256V1.12561C15.4751 0.57561 15.0251 0.12561 14.4751 0.12561ZM19.4751 4.12561H17.4751V13.1256H4.4751V15.1256C4.4751 15.6756 4.9251 16.1256 5.4751 16.1256H16.4751L20.4751 20.1256V5.12561C20.4751 4.57561 20.0251 4.12561 19.4751 4.12561Z"
        )
    );
    
    // Custom Icon from Figma. The source of this icon is currently unknown.
    // Need to ask. - Sewer
    public static readonly IconValue HardDrive = new AvaloniaSvg("avares://NexusMods.App.UI/Assets/Icons/disk_20px.svg");

    // The Black and White Nexus 'Developer' Logo.
    // This is the variation of the Nexus logo used in the App, and on the Discord.
    public static readonly IconValue Nexus = new AvaloniaSvg("avares://NexusMods.App.UI/Assets/nexus-logo-white.svg");

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue Stethoscope = new AvaloniaPathIcon(
        Geometry.Parse(
            "M13.8943 22.8744C12.0943 22.8744 10.561 22.2411 9.29429 20.9744C8.02762 19.7077 7.39429 18.1744 7.39429 16.3744V15.7994C5.96095 15.5661 4.76929 14.8952 3.81929 13.7869C2.86929 12.6786 2.39429 11.3744 2.39429 9.87439V3.87439H5.39429V2.87439H7.39429V6.87439H5.39429V5.87439H4.39429V9.87439C4.39429 10.9744 4.78595 11.9161 5.56929 12.6994C6.35262 13.4827 7.29429 13.8744 8.39429 13.8744C9.49429 13.8744 10.436 13.4827 11.2193 12.6994C12.0026 11.9161 12.3943 10.9744 12.3943 9.87439V5.87439H11.3943V6.87439H9.39429V2.87439H11.3943V3.87439H14.3943V9.87439C14.3943 11.3744 13.9193 12.6786 12.9693 13.7869C12.0193 14.8952 10.8276 15.5661 9.39429 15.7994V16.3744C9.39429 17.6244 9.83179 18.6869 10.7068 19.5619C11.5818 20.4369 12.6443 20.8744 13.8943 20.8744C15.1443 20.8744 16.2068 20.4369 17.0818 19.5619C17.9568 18.6869 18.3943 17.6244 18.3943 16.3744V14.6994C17.811 14.4994 17.3318 14.1411 16.9568 13.6244C16.5818 13.1077 16.3943 12.5244 16.3943 11.8744C16.3943 11.0411 16.686 10.3327 17.2693 9.74939C17.8526 9.16606 18.561 8.87439 19.3943 8.87439C20.2276 8.87439 20.936 9.16606 21.5193 9.74939C22.1026 10.3327 22.3943 11.0411 22.3943 11.8744C22.3943 12.5244 22.2068 13.1077 21.8318 13.6244C21.4568 14.1411 20.9776 14.4994 20.3943 14.6994V16.3744C20.3943 18.1744 19.761 19.7077 18.4943 20.9744C17.2276 22.2411 15.6943 22.8744 13.8943 22.8744ZM19.3943 12.8744C19.6776 12.8744 19.9151 12.7786 20.1068 12.5869C20.2985 12.3952 20.3943 12.1577 20.3943 11.8744C20.3943 11.5911 20.2985 11.3536 20.1068 11.1619C19.9151 10.9702 19.6776 10.8744 19.3943 10.8744C19.111 10.8744 18.8735 10.9702 18.6818 11.1619C18.4901 11.3536 18.3943 11.5911 18.3943 11.8744C18.3943 12.1577 18.4901 12.3952 18.6818 12.5869C18.8735 12.7786 19.111 12.8744 19.3943 12.8744Z"
        )
    );

    // From Design System "Custom Icons" section on Figma
    public static readonly IconValue ShieldHalfFull = new AvaloniaPathIcon(
        Geometry.Parse(
            "M21.4751 11.1256C21.4751 16.6756 17.6351 21.8656 12.4751 23.1256C7.3151 21.8656 3.4751 16.6756 3.4751 11.1256V5.12561L12.4751 1.12561L21.4751 5.12561V11.1256ZM12.4751 21.1256C16.2251 20.1256 19.4751 15.6656 19.4751 11.3456V6.42561L12.4751 3.30561V21.1256Z"
        )
    );

#endregion
}
