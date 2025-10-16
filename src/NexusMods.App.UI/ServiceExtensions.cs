using Microsoft.Extensions.DependencyInjection;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI;

public static class ServiceExtensions
{
    public static IServiceCollection AddView<TView, TViewModel>(this IServiceCollection services)
        where TView : class, IViewFor<TViewModel>
        where TViewModel : class, IViewModelInterface
    {
        services.AddTransient<IViewFor<TViewModel>, TView>();
        return services;
    }

    public static IServiceCollection AddViewModel<TVmImpl, TVmInterface>(this IServiceCollection services)
        where TVmImpl : AViewModel<TVmInterface>, TVmInterface
        where TVmInterface : class, IViewModelInterface
    {
        services.AddTransient<TVmInterface, TVmImpl>();
        return services;
    }
}
