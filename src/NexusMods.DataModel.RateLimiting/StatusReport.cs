// ReSharper disable NotAccessedPositionalProperty.Global
namespace NexusMods.DataModel.RateLimiting;

/// <summary>
/// Returns the current information about an <see cref="Resource{TResource,TUnit}"/>.
/// </summary>
/// <param name="Running">Number of currently running <see cref="IJob"/>(s) tied to this resource.</param>
/// <param name="Pending">Number of currently waiting <see cref="IJob"/>(s) to be started.</param>
/// <param name="Transferred">Total number of units transferred [data processed] </param>
/// <typeparam name="TUnit"></typeparam>
public record StatusReport<TUnit>(int Running, int Pending, TUnit Transferred);
