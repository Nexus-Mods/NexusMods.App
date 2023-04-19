using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.ViewModels.Helpers.ViewModelSelector;

public class ViewModelDesignSelector<TEnum, TVmType> : AViewModel<IViewModelSelector<TEnum, TVmType>>, IViewModelSelector<TEnum, TVmType> 
    where TVmType : class, IViewModelInterface 
    where TEnum : struct, Enum {
    private static readonly Dictionary<TEnum,Type> Mappings;
    private readonly Dictionary<TEnum,TVmType> _instances;

    [Reactive]
    public TEnum Current { get; set; }
    
    [Reactive]
    public TVmType ViewModel { get; set; }

    static ViewModelDesignSelector()
    {
        Mappings = AViewModelAttribute.GetAttributes<TEnum>();
    }

    public ViewModelDesignSelector(IEnumerable<TVmType> vms)
    {
        _instances = vms.ToDictionary(GetKeyForVm);
        Current = _instances.First().Key;
        ViewModel = _instances.First().Value;
    }
    
    private static TEnum GetKeyForVm(TVmType vm)
    {
        return Mappings.First(pair => vm.GetType().IsAssignableTo(pair.Value)).Key;
    }

    public void Set(TEnum current)
    {
        ViewModel = _instances[current];
        Current = current;
    }

    public IObservable<bool> IsActive(TEnum current)
    {
        return this.WhenAnyValue(vm => vm.Current)
            .Select(val => val.Equals(current));
    }
}
