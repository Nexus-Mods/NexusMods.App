using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;

namespace NexusMods.App.UI.ViewModels.Helpers.ViewModelSelector;

/// <summary>
/// Runtime ViewModelSelector, this switches between view models that are registered in the DI container.
/// </summary>
/// <typeparam name="TEnum"></typeparam>
/// <typeparam name="TVmType"></typeparam>
/// <typeparam name="TBase"></typeparam>
public class ViewModelSelector<TEnum, TVmType, TBase> : 
    AViewModelSelector<TEnum, TVmType, TBase>
    where TVmType : class, IViewModelInterface
    where TEnum : struct, Enum
{
    public ViewModelSelector(IServiceProvider serviceProvider) : 
        base(Mappings.Select(m => KeyValuePair.Create(m.Key, (TVmType)serviceProvider.GetRequiredService(m.Value)))
        .ToDictionary())
    {
    }
}
