using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;

namespace NexusMods.DataModel.Settings;

[JsonName("NexusMods.DataModel.Settings.DataStoreSettingsBackendEntity")]
internal record DataStoreSettingsBackendEntity : Entity
{
    public override EntityCategory Category => EntityCategory.GlobalSettings;

    public string Value { get; init; } = "null";
}
