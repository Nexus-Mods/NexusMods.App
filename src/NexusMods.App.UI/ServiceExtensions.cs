using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.ViewModels;
using ReactiveUI;

namespace NexusMods.App.UI;

public static class ServiceExtensions
{
    public static IServiceCollection AddView<TView, TViewModel>(this IServiceCollection services)
        where TView : class, IViewFor<TViewModel>
        where TViewModel : class
    {
        services.AddTransient<IViewFor<TViewModel>, TView>();
        return services;
    }

    public static IServiceCollection AddViewModel<TVMImpl, TVMInterface>(this IServiceCollection services)
        where TVMImpl : AViewModel<TVMInterface>, TVMInterface
        where TVMInterface : class, IViewModelInterface
    {
        services.AddTransient<TVMInterface, TVMImpl>();
        return services;
    }
}
