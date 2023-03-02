using Microsoft.Extensions.DependencyInjection;
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
}
