using NexusMods.DataModel.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModLists;

public abstract record AModFile(GamePath To) : Entity
{
    public override EntityCategory Category => EntityCategory.ModLists;
}