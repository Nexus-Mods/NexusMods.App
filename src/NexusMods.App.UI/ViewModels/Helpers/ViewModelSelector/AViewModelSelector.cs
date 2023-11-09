using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.ViewModels.Helpers.ViewModelSelector;


/// <summary>
/// Base class for view model selectors, defines most of the switching logic.
/// Design and runtime implementations are different in how they locate view models.
/// </summary>
/// <typeparam name="TEnum"></typeparam>
/// <typeparam name="TVmType"></typeparam>
/// <typeparam name="TBase"></typeparam>
public abstract class AViewModelSelector<TEnum, TVmType, TBase> :
    AViewModel<TBase>,
    IViewModelSelector<TEnum, TVmType>
    where TVmType : class, IViewModelInterface
    where TEnum : struct, Enum
    where TBase : class, IViewModelInterface
{
    protected static readonly Dictionary<TEnum,Type> Mappings;
    private readonly Dictionary<TEnum,TVmType> _instances;

    [Reactive]
    public TEnum Current { get; set; }

    [Reactive]
    public TVmType CurrentViewModel { get; set; }

    static AViewModelSelector()
    {
        Mappings = AViewModelAttribute.GetAttributes<TEnum>();
    }

    protected AViewModelSelector(Dictionary<TEnum, TVmType> instances)
    {
        _instances = instances;
        Current = _instances.First().Key;
        CurrentViewModel = _instances.First().Value;
    }

    public void Set(TEnum current)
    {
        CurrentViewModel = _instances[current];
        Current = current;
    }

    public IObservable<bool> IsActive(TEnum current)
    {
        return this.WhenAnyValue(vm => vm.Current)
            .Select(val => val.Equals(current));
    }

    public ICommand CommandForViewModel(TEnum current)
    {
        return ReactiveCommand.Create(() => Set(current));
    }
}
