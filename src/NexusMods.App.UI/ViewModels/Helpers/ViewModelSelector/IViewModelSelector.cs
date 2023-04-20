using System.Windows.Input;

namespace NexusMods.App.UI.ViewModels.Helpers.ViewModelSelector;

/// <summary>
/// A View model that can select between multiple ViewModels based on an Enum
/// </summary>
/// <typeparam name="TEnum"></typeparam>
/// <typeparam name="TVmType"></typeparam>
public interface IViewModelSelector<TEnum, TVmType> where TEnum : Enum where TVmType : class, IViewModelInterface
{
    /// <summary>
    /// Reactive property for the current ViewModel
    /// </summary>
    public TEnum Current { get; }
    
    /// <summary>
    /// The currently selected ViewModel
    /// </summary>
    public TVmType CurrentViewModel { get; }
    
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

    /// <summary>
    /// Get the command for the button that selects the given ViewModel
    /// </summary>
    /// <param name="current"></param>
    /// <returns></returns>
    public ICommand CommandFor(TEnum current);
}
