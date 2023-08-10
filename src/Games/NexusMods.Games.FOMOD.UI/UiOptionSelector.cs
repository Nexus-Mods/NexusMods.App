using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common.UserInput;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public sealed class UiOptionSelector : IOptionSelector, IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    private IServiceScope? _currentScope;
    private IGuidedInstallerWindowViewModel? _windowViewModel;

    public UiOptionSelector(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetupSelector(string windowName)
    {
        Debug.Assert(_currentScope is null);
        _currentScope = _serviceProvider.CreateScope();

        // TODO: create/show window?
        _windowViewModel = _currentScope.ServiceProvider.GetRequiredService<IGuidedInstallerWindowViewModel>();
        _windowViewModel.WindowName = windowName;
    }

    public void CleanupSelector()
    {
        if (_windowViewModel is not null)
        {
            // TODO: dispose & close window?
            _windowViewModel.ActiveStepViewModel = null;
            _windowViewModel = null;
        }

        _currentScope?.Dispose();
        _currentScope = null;
    }

    public Task<TOptionId[]> RequestChoice<TOptionId>(
        string query,
        ChoiceType type,
        Option<TOptionId>[] options)
    {
        throw new NotImplementedException();
    }

    public async Task<KeyValuePair<TGroupId, TOptionId[]>[]?> RequestMultipleChoices<TGroupId, TOptionId>(
        ChoiceGroup<TGroupId, TOptionId>[] choices)
    {
        Debug.Assert(_currentScope is not null);
        Debug.Assert(_windowViewModel is not null);

        _windowViewModel.ActiveStepViewModel ??= _currentScope.ServiceProvider.GetRequiredService<IGuidedInstallerStepViewModel>();

        var activeStepViewModel = _windowViewModel.ActiveStepViewModel;
        activeStepViewModel.AvailableChoices = CastChoices(choices);

        var tcs = new TaskCompletionSource<KeyValuePair<int, int[]>[]?>();
        activeStepViewModel.TaskCompletionSource = tcs;

        await tcs.Task;
        return CastResult<TGroupId, TOptionId>(tcs.Task.Result);
    }

    private static ChoiceGroup<int, int>[] CastChoices<TGroupId, TOptionId>(IEnumerable<ChoiceGroup<TGroupId, TOptionId>> choices)
    {
        // NOTE(erri120): FOMOD uses integer for IDs. This is the only thing currently supported.
        Debug.Assert(typeof(TGroupId) == typeof(int));
        Debug.Assert(typeof(TOptionId) == typeof(int));

        // TODO: verify this actually works
        return (ChoiceGroup<int, int>[])Convert.ChangeType(choices, typeof(ChoiceGroup<int, int>));
    }

    private static KeyValuePair<TGroupId, TOptionId[]>[]? CastResult<TGroupId, TOptionId>(KeyValuePair<int, int[]>[]? result)
    {
        // NOTE(erri120): FOMOD uses integer for IDs. This is the only thing currently supported.
        Debug.Assert(typeof(TGroupId) == typeof(int));
        Debug.Assert(typeof(TOptionId) == typeof(int));

        if (result is null) return null;

        // TODO: verify this actually works
        return (KeyValuePair<TGroupId, TOptionId[]>[]?)Convert.ChangeType(result, typeof(KeyValuePair<TGroupId, TOptionId[]>[]));
    }

    public void Dispose() => CleanupSelector();
}
