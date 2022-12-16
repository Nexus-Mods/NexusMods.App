using NexusMods.DataModel.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts;

public abstract record AModFile : Entity
{
    public override EntityCategory Category => EntityCategory.ModLists;
    public required GamePath To { get; init; }
}