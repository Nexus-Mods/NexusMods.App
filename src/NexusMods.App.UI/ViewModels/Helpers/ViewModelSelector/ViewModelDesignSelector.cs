using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.ViewModels.Helpers.ViewModelSelector;

/// <summary>
/// ViewModelSelector for design time, this switches between hardcoded view models.
/// </summary>
/// <typeparam name="TEnum"></typeparam>
/// <typeparam name="TVmType"></typeparam>
/// <typeparam name="TBase"></typeparam>
public class ViewModelDesignSelector<TEnum, TVmType, TBase> : 
    AViewModelSelector<TEnum, TVmType, TBase>
    where TVmType : class, IViewModelInterface
    where TEnum : struct, Enum {
    private static readonly Dictionary<TEnum,Type> Mappings;

    static ViewModelDesignSelector()
    {
        Mappings = AViewModelAttribute.GetAttributes<TEnum>();
    }

    protected ViewModelDesignSelector(params TVmType[] vms) : base(vms.ToDictionary(GetKeyForVm))
    {
    }
    
    private static TEnum GetKeyForVm(TVmType vm)
    {
        return Mappings.First(pair => vm.GetType().IsAssignableTo(pair.Value)).Key;
    }
}
