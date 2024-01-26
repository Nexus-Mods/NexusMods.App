using System.Numerics;

namespace NexusMods.Abstractions.Activities;

/// <summary>
/// An abstract factory for creating activities.
/// </summary>
public interface IActivityFactory
{
    /// <summary>
    /// Creates a new activity with the specified template and arguments.
    /// </summary>
    /// <param name="group"></param>
    /// <param name="template"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    public IActivitySource Create(ActivityGroup group, string template, params object[] arguments);


    /// <summary>
    /// Overload for <see cref="Create(ActivityGroup,string,object[])"/> that accepts a payload.
    /// </summary>
    /// <param name="group"></param>
    /// <param name="payload"></param>
    /// <param name="template"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    public IActivitySource CreateWithPayload(ActivityGroup group, object payload, string template,
        params object[] arguments);

    /// <summary>
    /// Creates a new activity with the specified template and arguments. And a progress value of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="group"></param>
    /// <param name="template"></param>
    /// <param name="arguments"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IActivitySource<T> Create<T>(ActivityGroup group, string template, params object[] arguments)
        where T : struct, IDivisionOperators<T, T, double>, IAdditionOperators<T, T, T>,
        IDivisionOperators<T, double, T>, ISubtractionOperators<T, T, T>;
}
