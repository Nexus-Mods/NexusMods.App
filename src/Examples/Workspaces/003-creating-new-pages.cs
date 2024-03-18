using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI.Fody.Helpers;
// ReSharper disable All

namespace Examples.Workspaces;

/*
 * This example is on how to create new Pages. You need the following types:
 * - An interface that extends IPageViewModelInterface and a class that
 *   implements that interface.
 * - A record that implements IPageFactoryContext
 * - A class that inherits from APageFactory
 */


// This is the View Model Interface of the Page. Note that it doesn't extend
// IViewModelInterface but the special IPageViewModelInterface.
file interface IMyPageViewModel : IPageViewModelInterface
{
    public string Name { get; set; }
}

// This is the implementation of IMyPageViewModel. Similarly, it doesn't inherit
// from AViewModel<TVM> but from APageViewModel<TVM>. This special abstract
// class exposes some helpful properties and methods for Page-specific operations.
file class MyPageViewModel : APageViewModel<IMyPageViewModel>, IMyPageViewModel
{
    [Reactive] public string Name { get; set; } = string.Empty;

    public MyPageViewModel(IWindowManager windowManager) : base(windowManager) { }
}

// Every Page needs a Context. This Context MUST be serializable, so don't forget
// to put the JsonName attribute on it AND register it in your ITypeFiner.
[JsonName("Examples.Workspaces.MyPageContext")]
file record MyPageContext : IPageFactoryContext
{
    public required string Name { get; init; }
}

// Page factories should ideally inherit from APageFactory<TVM, TContext> as it's
// strongly typed using generics.
file class MyPageFactory : APageFactory<IMyPageViewModel, MyPageContext>
{
    public MyPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    // While it's not required, it's best practices to have this StaticId field
    // so others can refer to this factory by Id statically.
    // IMPORTANT: The Guid MUST be hardcoded and MUST NOT be a duplicate.
    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse(""));
    public override PageFactoryId Id => StaticId;

    public override IMyPageViewModel CreateViewModel(MyPageContext context)
    {
        // You can implement this method in whatever way makes sense for the
        // given page. In this example we're using DI to get the implementation
        // of IMyPageViewModel and then we set the properties to that of the
        // Context.
        // Alternatively, you could just call the constructor directly and pass
        // the Context values directly. This is recommended if the View Model
        // is read-only and the values don't change.
        var viewModel = ServiceProvider.GetRequiredService<IMyPageViewModel>();
        viewModel.Name = context.Name;
        return viewModel;
    }
}
