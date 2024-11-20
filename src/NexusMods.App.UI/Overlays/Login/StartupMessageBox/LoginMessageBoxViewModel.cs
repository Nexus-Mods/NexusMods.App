using System.Reactive;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Settings;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Login;

public class LoginMessageBoxViewModel : AOverlayViewModel<ILoginMessageBoxViewModel>, ILoginMessageBoxViewModel
{
    private readonly ISettingsManager _settingsManager;
    private readonly IOverlayController _overlayController;
    private readonly ILoginManager _loginManager;

    public LoginMessageBoxViewModel(ISettingsManager settingsManager, ILoginManager loginManager, IOverlayController overlayController)
    {
        _settingsManager = settingsManager;
        _overlayController = overlayController;
        _loginManager = loginManager;
        
        OkCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            Close();
            await loginManager.LoginAsync();
        });

        CancelCommand = ReactiveCommand.Create(Close);
    }

    public ReactiveCommand<Unit, Unit> OkCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    
    public bool MaybeShow()
    {
        // If we're already logged in via OAuth, don't show the login modal
        if (_loginManager.IsOAuthLogin) 
            return false;
        
        // If we've already shown the modal, don't show it again
        if (_settingsManager.Get<LoginSettings>().HasShownModal) return false;
        _settingsManager.Update<LoginSettings>(settings => settings with { HasShownModal = true });
        
        _overlayController.Enqueue(this);
        return true;
    }
}
