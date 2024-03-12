using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

// TODO: Show in Settings (https://github.com/Nexus-Mods/NexusMods.App/issues/396)
// Related: https://github.com/Nexus-Mods/NexusMods.App/issues/946

[PublicAPI]
public class OpenPageBehaviorSettings : Dictionary<NavigationInput, OpenPageBehaviorType>;
