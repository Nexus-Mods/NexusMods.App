namespace NexusMods.App.UI.WorkspaceSystem;

/// <summary>
/// Represents a factory for creating pages.
/// </summary>
public interface IPageFactory
{
    /// <summary>
    /// Gets the unique identifier of this factory.
    /// </summary>
    public PageFactoryId Id { get; }

    /// <summary>
    /// Creates a new page using the provided parameter
    /// </summary>
    public Page Create(IPageFactoryParameter parameter);
}

/// <summary>
/// Generic implementation of <see cref="IPageFactory"/>
/// </summary>
/// <typeparam name="TViewModel"></typeparam>
/// <typeparam name="TParameter"></typeparam>
public interface IPageFactory<out TViewModel, in TParameter> : IPageFactory
    where TViewModel : class, IViewModel
    where TParameter : class, IPageFactoryParameter
{
    Page IPageFactory.Create(IPageFactoryParameter parameter)
    {
        if (parameter is not TParameter actualParameter)
            throw new ArgumentException($"Unsupported type: {parameter.GetType()}");

        var vm = CreateViewModel(actualParameter);
        return new Page
        {
            ViewModel = vm,
            PageData = new PageData
            {
                FactoryId = Id,
                Parameter = actualParameter
            }
        };
    }

    /// <summary>
    /// Creates a new view model using the provided parameter.
    /// </summary>
    public TViewModel CreateViewModel(TParameter parameter);
}

