using JetBrains.Annotations;
using NexusMods.Sdk.Settings;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.UI.Sdk.Settings;

[PublicAPI]
public record SectionDescriptor(
    SectionId Id,
    string Name,
    Func<IconValue> IconFunc,
    uint Priority = 0,
    bool Hidden = false
);
