using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.Interprocess.Jobs;

public abstract record AJobEntity : Entity
{
    public override EntityCategory Category => EntityCategory.InterprocessJob;
}
