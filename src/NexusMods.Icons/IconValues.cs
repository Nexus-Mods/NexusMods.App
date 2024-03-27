using Avalonia.Media;

namespace NexusMods.Icons;

// https://www.figma.com/file/8pjtQeNggvVi7RWoLNGV80/%F0%9F%A7%B0-Nexus-Mods-Design-System?type=design&node-id=130-463

public static class IconValues
{
#region Action

    // https://pictogrammers.com/library/mdi/icon/delete-outline/
    public static readonly IconValue DeleteOutline = new ProjektankerIcon("mdi-delete-outline");

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

    // https://pictogrammers.com/library/mdi/icon/open-in-new/
    public static readonly IconValue OpenInNew = new ProjektankerIcon("mdi-open-in-new");

    // https://pictogrammers.com/library/mdi/icon/tab/
    public static readonly IconValue Tab = new ProjektankerIcon("mdi-tab");

    // https://pictogrammers.com/library/mdi/icon/magnfiy/
    public static readonly IconValue Search = new ProjektankerIcon("mdi-magnify");

    // https://pictogrammers.com/library/mdi/icon/cog/
    public static readonly IconValue Settings = new ProjektankerIcon("mdi-cog");

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

#endregion

#region Content

    // https://pictogrammers.com/library/mdi/icon/plus/
    public static readonly IconValue Add = new ProjektankerIcon("mdi-plus");

    // https://pictogrammers.com/library/mdi/icon/plus-circle/
    public static readonly IconValue AddCircle = new ProjektankerIcon("mdi-plus-circle");

    // https://pictogrammers.com/library/mdi/icon/plus-circle-outline/
    public static readonly IconValue AddCircleOutline = new ProjektankerIcon("mdi-plus-circle-outline");

    // https://pictogrammers.com/library/mdi/icon/redo/
    public static readonly IconValue Redo = new ProjektankerIcon("mdi-redo");

    // https://pictogrammers.com/library/mdi/icon/minus-circle-outline/
    public static readonly IconValue RemoveCircleOutline = new ProjektankerIcon("mdi-minus-circle-outline");

    // https://pictogrammers.com/library/mdi/icon/undo/
    public static readonly IconValue Undo = new ProjektankerIcon("mdi-undo");

#endregion

#region Editor

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

    // https://pictogrammers.com/library/mdi/icon/video-outline/
    public static readonly IconValue Video = new ProjektankerIcon("mdi-video-outline");

    // https://pictogrammers.com/library/mdi/icon/music-note/
    public static readonly IconValue MusicNote = new ProjektankerIcon("mdi-music-note");

    // https://pictogrammers.com/library/mdi/icon/file-document-outline/
    public static readonly IconValue FileDocumentOutline = new ProjektankerIcon("mdi-file-document-outline");

#endregion

#region Hardware

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

    // https://pictogrammers.com/library/mdi/icon/close/
    public static readonly IconValue Close = new ProjektankerIcon("mdi-close");

    // https://pictogrammers.com/library/mdi/icon/refresh/
    public static readonly IconValue Refresh = new ProjektankerIcon("mdi-refresh");

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
    public static readonly IconValue Collections = new AvaloniaPathIcon(
        Geometry.Parse(
            "M12.1979 15.4946L6.68644 11.2096L5.47498 12.1518L12.2053 17.3866L18.9357 12.1518L17.7167 11.2021L12.1979 15.4946ZM12.1979 19.2336L6.68644 14.9486L5.47498 15.8908L12.2053 21.1255L18.9357 15.8908L17.7167 14.9411L12.1979 19.2336ZM12.2053 13.5951L17.7093 9.31006L18.9357 8.36033L12.2053 3.12561L5.47498 8.36033L6.69392 9.31006L12.2053 13.5951Z"
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
    public static readonly IconValue DiagnosticPage = new AvaloniaPathIcon(
        Geometry.Parse(
            "M2 9.87439V6.87439C2 6.32439 2.19583 5.85356 2.5875 5.46189C2.97917 5.07022 3.45 4.87439 4 4.87439H20C20.55 4.87439 21.0208 5.07022 21.4125 5.46189C21.8042 5.85356 22 6.32439 22 6.87439V9.87439H20V6.87439H4V9.87439H2ZM4 20.8744C3.45 20.8744 2.97917 20.6786 2.5875 20.2869C2.19583 19.8952 2 19.4244 2 18.8744V15.8744H4V18.8744H20V15.8744H22V18.8744C22 19.4244 21.8042 19.8952 21.4125 20.2869C21.0208 20.6786 20.55 20.8744 20 20.8744H4ZM10 17.8744C10.1833 17.8744 10.3583 17.8286 10.525 17.7369C10.6917 17.6452 10.8167 17.5077 10.9 17.3244L14 11.1244L15.1 13.3244C15.1833 13.5077 15.3083 13.6452 15.475 13.7369C15.6417 13.8286 15.8167 13.8744 16 13.8744H22V11.8744H16.625L14.9 8.42439C14.8167 8.24106 14.6917 8.11189 14.525 8.03689C14.3583 7.96189 14.1833 7.92439 14 7.92439C13.8167 7.92439 13.6417 7.96189 13.475 8.03689C13.3083 8.11189 13.1833 8.24106 13.1 8.42439L10 14.6244L8.9 12.4244C8.81667 12.2411 8.69167 12.1036 8.525 12.0119C8.35833 11.9202 8.18333 11.8744 8 11.8744H2V13.8744H7.375L9.1 17.3244C9.18333 17.5077 9.30833 17.6452 9.475 17.7369C9.64167 17.8286 9.81667 17.8744 10 17.8744Z"
        )
    );

#endregion
}
