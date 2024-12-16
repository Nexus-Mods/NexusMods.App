using System.Diagnostics.CodeAnalysis;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NexusMods.Telemetry;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public static class Events
{
    public static class Game
    {
        private const string Category = "Game";
        public static readonly EventDefinition AddGame    = new(Category, Action: "Add Game");
        public static readonly EventDefinition ViewGame   = new(Category, Action: "View Game");
        public static readonly EventDefinition RemoveGame = new(Category, Action: "Remove Game");
    }

    public static class Loadout
    {
        private const string Category = "Loadout";
        public static readonly EventDefinition CreateLoadout         = new(Category, Action: "Create Loadout");
        public static readonly EventDefinition CreateLoadoutCopy     = new(Category, Action: "Create Loadout Copy");
        public static readonly EventDefinition ApplyLoadoutChanges   = new(Category, Action: "Apply Loadout Changes");
        public static readonly EventDefinition PreviewLoadoutChanges = new(Category, Action: "Preview Loadout Changes");
    }

    public static class Library
    {
        private const string Category = "Library";
    }

    public static class HealthCheck
    {
        private const string Category = "Health check";
        public static readonly EventDefinition ViewHealthCheckItem = new(Category, Action: "View health check item");
    }

    public static class Page
    {
        private const string Category = "Page";
        public static readonly EventDefinition ReplaceTab = new(Category, Action: "Replace Tab");
        public static readonly EventDefinition NewTab = new(Category, Action: "New Tab");
        public static readonly EventDefinition NewPanel = new(Category, Action: "New Panel");
    }

    public static class PageHistory
    {
        private const string Category = "Page History";
        public static readonly EventDefinition Back = new(Category, Action: "Back");
        public static readonly EventDefinition Forward = new(Category, Action: "Forward");
    }

    public static class Help
    {
        private const string Category = "Help";
        public static readonly EventDefinition ViewChangelog = new(Category, Action: "View changelog");
        public static readonly EventDefinition ViewAppLogs   = new(Category, Action: "View app logs");
        public static readonly EventDefinition GiveFeedback  = new(Category, Action: "Give feedback");
    }
}
