namespace NexusMods.Abstractions.DurableJobs;
using TransparentValueObjects;

[ValueObject<Guid>]
public readonly partial struct JobId : IAugmentWith<JsonAugment>
{
    /// <summary>
    /// Default null value for <see cref="JobId"/>.
    /// </summary>
    public static JobId Empty { get; } = new(Guid.Empty);
}
