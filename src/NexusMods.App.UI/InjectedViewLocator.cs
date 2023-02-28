using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace NexusMods.App.UI;

public class InjectedViewLocator : IViewLocator
{
    private readonly IServiceProvider _provider;
    private readonly MethodInfo _method;
    private readonly ILogger<InjectedViewLocator> _logger;

    public InjectedViewLocator(ILogger<InjectedViewLocator> logger, IServiceProvider provider)
    {
        _logger = logger;
        _provider = provider;
        _method = this.GetType().GetMethod("ResolveViewInner", BindingFlags.NonPublic | BindingFlags.Instance)!;
    }
    
    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
    {
        try
        {
            var method = _method.MakeGenericMethod(viewModel?.GetType() ?? typeof(object));
            return (IViewFor?)method.Invoke(this, Array.Empty<object>());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to resolve view for {ViewModel}", typeof(T).FullName);
            return null;
        }
    }

    private IViewFor? ResolveViewInner<T>() where T : class
    {
        return _provider.GetRequiredService<IViewFor<T>>();
    }
}