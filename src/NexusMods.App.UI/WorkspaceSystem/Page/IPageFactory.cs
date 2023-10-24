namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPageFactoryParameter { }

public interface IPageFactory
{
    /// <summary>
    /// Gets the unique identifier of this factory.
    /// </summary>
    public PageFactoryId Id { get; }

    /// <summary>
    /// Creates a new page using the provided parameter
    /// </summary>
    public IPage? Create(object parameter);
}

public interface IPageFactory<out TPage, in TParameter> : IPageFactory
    where TPage : class, IPage
    where TParameter : class, IPageFactoryParameter
{
    IPage? IPageFactory.Create(object parameter)
    {
        if (parameter is not TParameter actualParameter)
            throw new ArgumentException($"Unsupported type: {parameter.GetType()}");
        return Create(actualParameter);
    }

    /// <summary>
    /// Creates a new view model using the provided parameter.
    /// </summary>
    public TPage? Create(TParameter parameter);
}
