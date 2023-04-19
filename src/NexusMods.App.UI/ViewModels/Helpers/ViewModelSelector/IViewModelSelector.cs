namespace NexusMods.App.UI.ViewModels.Helpers.ViewModelSelector;

public interface IViewModelSelector<TEnum, TVmType> where TEnum : Enum where TVmType : class, IViewModelInterface
{
    /// <summary>
    /// Reactive property for the current ViewModel
    /// </summary>
    public TEnum Current { get; }
    
    /// <summary>
    /// The currently selected ViewModel
    /// </summary>
    public TVmType ViewModel { get; }
    
    /// <summary>
    /// Select a ViewModel
    /// </summary>
    /// <param name="current"></param>
    public void Set(TEnum current);
    
    /// <summary>
    /// Observable that fires to true/false when the given ViewModel is of
    /// the type that matches `current`
    /// </summary>
    /// <param name="current"></param>
    /// <returns></returns>
    public IObservable<bool> IsActive(TEnum current);
}
