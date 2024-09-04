namespace NexusMods.Abstractions.DurableJobs;
using TransparentValueObjects;

[ValueObject<Guid>]
public readonly partial struct JobId : IAugmentWith<JsonAugment>, IAugmentWith<DefaultValueAugment>
{
    /// <summary>
    /// Default value for <see cref="JobId"/>.
    /// </summary>
    public static JobId DefaultValue { get; } = From(Guid.Empty);
}
