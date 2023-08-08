using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;
using NexusMods.Common.UserInput;

namespace NexusMods.Games.FOMOD;

public class InstallerDelegates : ICoreDelegates
{
    public IPluginDelegates plugin => new PluginDelegates();

    public IContextDelegates context => throw new NotImplementedException();

    public IIniDelegates ini => throw new NotImplementedException();

    public IUIDelegates ui { get; init; }

#pragma warning disable CS0169
    private IOptionSelector? _inputHandler;
#pragma warning restore CS0169

    public InstallerDelegates(IOptionSelector optionSelector)
    {
        ui = new UiDelegate(optionSelector);
    }
}

// TODO: This is a temporary implementation to avoid crashing on FOMOD installers that use these methods.
public class PluginDelegates : IPluginDelegates
{
    public Task<string[]> GetAll(bool activeOnly)
    {
        return Task.FromResult(Array.Empty<string>());
    }

    public Task<bool> IsActive(string pluginName)
    {
        return Task.FromResult(true);
    }

    public Task<bool> IsPresent(string pluginName)
    {
        return Task.FromResult(true);
    }
}

public class UiDelegate : IUIDelegates
{
    private IOptionSelector _optionSelector;
    private Action<int, int, int[]> _select = null!;
    private Action<bool, int> _cont = null!;

    public UiDelegate(IOptionSelector optionSelector)
    {
        _optionSelector = optionSelector;
    }

    public void EndDialog()
    {
        // throw new NotImplementedException();
    }

    public void ReportError(string title, string message, string details)
    {
        throw new NotImplementedException();
    }

    public void StartDialog(string? moduleName, HeaderImage image, Action<int, int, int[]> select, Action<bool, int> cont, Action cancel)
    {
        _select = select;
        _cont = cont;
    }

    public void UpdateState(InstallerStep[] installSteps, int currentStep)
    {
        _optionSelector.RequestMultipleChoices(ToChoices(installSteps[currentStep].optionalFileGroups))
            .ContinueWith<Tuple<int, IEnumerable<int>>?>(choiceDict =>
            {
                var kv = choiceDict.Result;
                if (kv == null)
                    _cont(true, currentStep);
                else
                    _select(currentStep, kv.Item1, kv.Item2.ToArray());

                return null;
            });
    }

    private IEnumerable<ChoiceGroup<int, int>> ToChoices(GroupList groups)
    {
        return groups.group.Select(group => new ChoiceGroup<int, int>
        {
            Id = group.id,
            Type = ConvertChoiceType(group.type),
            Query = group.name,
            Options = ToOptions(group.options),
        });
    }

    private IEnumerable<Option<int>> ToOptions(IEnumerable<Option> options)
    {
        return options.Select(option => new Option<int>
        {
            Id = option.id,
            Name = option.name,
            Description = option.description,
            ImageUrl = option.image != null ? AssetUrl.From(option.image) : null,
            Type = MakeOptionState(option),
        }).ToArray();
    }

    private OptionState MakeOptionState(Option option)
    {
        var state = option.type switch
        {
            "Required" => OptionState.Required,
            "NotUsable" => OptionState.Disabled,
            "Recommended" => OptionState.Selected,
            _ => OptionState.Available
        };

        if ((state == OptionState.Available) && option.selected)
            state = OptionState.Selected;

        return state;
    }

    private ChoiceType ConvertChoiceType(string input)
    {
        return input switch
        {
            "SelectAtLeastOne" => ChoiceType.AtLeastOne,
            "SelectAtMostOne" => ChoiceType.AtMostOne,
            "SelectExactlyOne" => ChoiceType.ExactlyOne,
            _ => ChoiceType.Any
        };
    }
}
