using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Interprocess.Jobs;

/// <summary>
/// A entity that represents a interprocess job
/// </summary>
public abstract record AJobEntity : Entity
{
    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.InterprocessJob;
}
