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
        if (_settingsManager.Get<LoginSettings>().HasShownModal) return false;
        _settingsManager.Update<LoginSettings>(settings => settings with { HasShownModal = true });
        
        if (_loginManager.IsLoggedIn)
        {
            return false;
        }
        
        _overlayController.Enqueue(this);
        return true;
    }
}
