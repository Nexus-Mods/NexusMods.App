using Microsoft.Extensions.DependencyInjection;
using NexusMods.Sdk.EventBus;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CLI;
using NexusMods.Sdk;
using R3;

namespace NexusMods.App.UI.Pages;

public sealed class ProtocolRegistrationTestPageViewModel : APageViewModel<IProtocolRegistrationTestPageViewModel>, IProtocolRegistrationTestPageViewModel, IDisposable
{
    public ReactiveCommand CommandStartTest { get; }
    public ReactiveCommand CommandStopTest { get; }

    private readonly BindableReactiveProperty<bool> _isTestRunning = new();
    public IReadOnlyBindableReactiveProperty<bool> IsTestRunning => _isTestRunning;

    private readonly BindableReactiveProperty<bool> _hasTestResult = new();
    public IReadOnlyBindableReactiveProperty<bool> HasTestResult => _hasTestResult;

    private readonly BindableReactiveProperty<bool> _failedTest = new();
    public IReadOnlyBindableReactiveProperty<bool> FailedTest => _failedTest;

    private Guid _testRunId = Guid.NewGuid();
    private readonly IDisposable _disposable;

    public ProtocolRegistrationTestPageViewModel(IServiceProvider serviceProvider, IWindowManager windowManager) : base(windowManager)
    {
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();
        var osInterop = serviceProvider.GetRequiredService<IOSInterop>();

        CommandStartTest = IsTestRunning.AsObservable().Select(x => !x).ToReactiveCommand(_ =>
        {
            _isTestRunning.Value = true;
            _hasTestResult.Value = false;

            _testRunId = Guid.NewGuid();
            osInterop.OpenUri(new Uri($"nxm://protocol-test/?id={_testRunId}"));
        });

        CommandStopTest = IsTestRunning.AsObservable().ToReactiveCommand(_ => _isTestRunning.Value = false);

        _disposable = eventBus.ObserveMessages<CliMessages.TestProtocolRegistration>().Subscribe(this, static (message, self) =>
        {
            self._isTestRunning.Value = false;
            self._hasTestResult.Value = true;
            self._failedTest.Value = message.Id != self._testRunId;
        });
    }

    private bool _isDisposed;
    public void Dispose()
    {
        if (_isDisposed) return;
        _disposable.Dispose();
        _isDisposed = true;
    }
}
