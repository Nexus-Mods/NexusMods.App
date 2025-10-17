using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI;

public class InjectedViewLocator : IViewLocator
{
    private readonly IServiceProvider _provider;
    private readonly MethodInfo _resolveViewInnerMethod;
    private readonly ILogger<InjectedViewLocator> _logger;

    public InjectedViewLocator(ILogger<InjectedViewLocator> logger, IServiceProvider provider)
    {
        _logger = logger;
        _provider = provider;
        _resolveViewInnerMethod = GetType().GetMethod(nameof(ResolveViewInner), BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    [SuppressMessage("ReSharper", "HeapView.PossibleBoxingAllocation", Justification = "Our ViewModels are always reference types.")]
    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
    {
        if (viewModel is null)
            return null;

        _logger.FindingView(viewModel.GetType().FullName ?? viewModel.GetType().ToString());
        try
        {
            if (viewModel is IViewModel vm)
            {
                var intType = vm.ViewModelInterface;
                var method = _resolveViewInnerMethod.MakeGenericMethod(intType);
                var view = (IViewFor?)method.Invoke(this, Array.Empty<object>());
                if (view is IViewContract vc && contract is not null)
                {
                    vc.ViewContract = contract;
                }
                return view;
            }
            _logger.LogError("Failed to resolve view for {ViewModel}", typeof(T).FullName);
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to resolve view for {ViewModel}", typeof(T).FullName);
            return null;
        }
    }

    /// <summary>
    /// This is a helper method used to simplify the casting involved in
    /// creating a view for a given view model. This is not dead code or typed
    /// incorrectly, it is used by the <see cref="ResolveView{T}"/> method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once ReturnTypeCanBeNotNullable
    private IViewFor? ResolveViewInner<T>() where T : class
    {
        return _provider.GetRequiredService<IViewFor<T>>();
    }
}
