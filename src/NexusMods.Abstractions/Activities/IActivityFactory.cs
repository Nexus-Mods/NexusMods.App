namespace NexusMods.DataModel.Activities;

/// <summary>
/// An abstract factory for creating activities.
/// </summary>
public interface IActivityFactory
{
    /// <summary>
    /// Creates a new activity with the specified template and arguments.
    /// </summary>
    /// <param name="template"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    public IActivitySource Create(string template, params object[] arguments);

    /// <summary>
    /// Creates a new activity with the specified template and arguments. And a progress value of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="template"></param>
    /// <param name="arguments"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IActivitySource<T> Create<T>(string template, params object[] arguments);
}
