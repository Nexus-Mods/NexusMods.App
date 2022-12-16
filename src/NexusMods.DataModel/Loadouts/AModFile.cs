using NexusMods.DataModel.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts;

public abstract record AModFile : Entity
{
    public override EntityCategory Category => EntityCategory.Loadouts;
    public required GamePath To { get; init; }
}