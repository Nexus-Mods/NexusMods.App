using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Logging;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.DevelopmentBuildBanner;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.AlphaWarning;
using NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;
using NexusMods.App.UI.Overlays.Login;
using NexusMods.App.UI.Overlays.MetricsOptIn;
using NexusMods.App.UI.Overlays.Updater;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Tmds.DBus.Protocol;

namespace NexusMods.App.UI.Windows;

public class MainWindowViewModel : AViewModel<IMainWindowViewModel>, IMainWindowViewModel
{
    private readonly IWindowManager _windowManager;
    
    public ReactiveCommand<Unit, Unit> BringWindowToFront { get; } = ReactiveCommand.Create(() => { });

    public MainWindowViewModel(
        IServiceProvider serviceProvider,
        IWindowManager windowManager,
        IOverlayController overlayController,
        ILoginManager loginManager)
    {
        // NOTE(erri120): can't use DI for VMs that require an active Window because
        // those VMs would be instantiated before this constructor gets called.
        // Use GetRequiredService<TVM> instead.
        _windowManager = windowManager;
        _windowManager.RegisterWindow(this);
        
        WorkspaceController = new WorkspaceController(
            window: this,
            serviceProvider: serviceProvider
        );

        TopBar = serviceProvider.GetRequiredService<ITopBarViewModel>();
        TopBar.AddPanelDropDownViewModel = new AddPanelDropDownViewModel(WorkspaceController);

        Spine = serviceProvider.GetRequiredService<ISpineViewModel>();
        DevelopmentBuildBanner = serviceProvider.GetRequiredService<IDevelopmentBuildBannerViewModel>();
        
        this.WhenActivated(d =>
        {
            ConnectErrors(serviceProvider)
                .DisposeWith(d);
            
            var alphaWarningViewModel = serviceProvider.GetRequiredService<IAlphaWarningViewModel>();
            alphaWarningViewModel.WorkspaceController = WorkspaceController;
            alphaWarningViewModel.Controller = overlayController;
            alphaWarningViewModel.MaybeShow();

            var metricsOptInViewModel = serviceProvider.GetRequiredService<IMetricsOptInViewModel>();
            metricsOptInViewModel.Controller = overlayController;

            // Only show the updater if the metrics opt-in has been shown before, so we don't spam the user.
            if (!metricsOptInViewModel.MaybeShow())
            {
                var updaterViewModel = serviceProvider.GetRequiredService<IUpdaterViewModel>();
                updaterViewModel.MaybeShow();
            }

            loginManager.IsLoggedInObservable
                .DistinctUntilChanged()
                .Where(isSignedIn => isSignedIn)
                .Select(_ => Unit.Default)
                .InvokeReactiveCommand(BringWindowToFront)
                .DisposeWith(d);
            
            var loginMessageVM = serviceProvider.GetRequiredService<ILoginMessageBoxViewModel>();
            loginMessageVM.Controller = overlayController;
            loginMessageVM.MaybeShow();

            this.WhenAnyValue(vm => vm.Spine.LeftMenuViewModel)
                .BindToVM(this, vm => vm.LeftMenu)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.IsActive)
                .Where(isActive => isActive)
                .Select(_ => this)
                .BindTo(_windowManager, manager => manager.ActiveWindow)
                .DisposeWith(d);
            
            overlayController.WhenAnyValue(oc => oc.CurrentOverlay)
                .BindTo(this, vm => vm.CurrentOverlay)
                .DisposeWith(d);

            Disposable.Create(this, vm =>
            {
                vm._windowManager.UnregisterWindow(vm);
            }).DisposeWith(d);

            if (!_windowManager.RestoreWindowState(this))
            {
                // NOTE(erri120): select home on startup if we didn't restore the previous state
                Spine.NavigateToHome();
            }
        });
    }

    private IDisposable ConnectErrors(IServiceProvider provider)
    {
        var source = provider.GetService<IObservableExceptionSource>();
        if (source is null)
            return Disposable.Empty;

        return source.Exceptions
            .Subscribe(msg =>
                {
                    if (!ShouldShowError(msg))
                        return;
                    
                    var title = "Unhandled Exception";
                    var description = msg.Message;
                    string? details = null;
                    if (msg.Exception != null)
                    {
                        details = $"""
                                  ```
                                  {msg.Exception}
                                  ``` 
                                  """;
                    }

                    Task.Run(() => MessageBoxOkViewModel.Show(provider, title, description, details));
                }
            );
    }

    private static bool ShouldShowError(LogMessage msg)
    {
        // Note(sewer): On some Wayland compositors on Linux, there is currently a bug 
        // where showing tooltips can trigger an exception dialog.
        //
        //   System.AggregateException: A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread. (org.freedesktop.DBus.Error.ServiceUnknown: The name is not activatable)
        //    ---> Tmds.DBus.Protocol.DBusException: org.freedesktop.DBus.Error.ServiceUnknown: The name is not activatable
        //      at Tmds.DBus.Protocol.DBusConnection.MyValueTaskSource`1.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(Int16 token)
        //      at Tmds.DBus.Protocol.DBusConnection.CallMethodAsync(MessageBuffer message)
        //      at Tmds.DBus.Protocol.Connection.CallMethodAsync(MessageBuffer message)
        //      --- End of inner exception stack trace ---
        //
        // This needs to be fixed on Avalonia's end, unfortunately, which may take a while
        // and wouldn't quite cut it for Stardew Valley Beta.
        //
        // For now, we opt to ignore this exception for the time being, while either us
        // or the Avalonia folks come up with a solution. Whichever is first.
        // ReSharper disable once MergeIntoPattern
        if (msg.Exception is AggregateException aggregateException 
            && aggregateException.InnerException is DBusException)
            return false;

        return true;
    }

    internal void OnClose()
    {
        // NOTE(erri120): This gets called by the View and can't be inside the disposable
        // of the VM because the MainWindowViewModel gets disposed last, after its contents.
        _windowManager.SaveWindowState(this);
    }
    
    public WindowId WindowId { get; } = WindowId.NewId();

    /// <inheritdoc/>
    [Reactive] public bool IsActive { get; set; }
    
    [Reactive]
    public IOverlayViewModel? CurrentOverlay { get; set; }

    /// <inheritdoc/>
    public IWorkspaceController WorkspaceController { get; }


    [Reactive] public ISpineViewModel Spine { get; set; }

    [Reactive] public ILeftMenuViewModel? LeftMenu { get; set; }

    [Reactive]
    public ITopBarViewModel TopBar { get; set; }

    [Reactive]
    public IDevelopmentBuildBannerViewModel DevelopmentBuildBanner { get; set; }
}
