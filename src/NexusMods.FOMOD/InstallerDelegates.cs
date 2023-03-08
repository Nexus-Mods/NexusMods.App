using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;
using NexusMods.Common;
using System.Security.Cryptography;

namespace NexusMods.FOMOD;

public class UIDelegate : IUIDelegates
{
    private IUserInput _userInput;
    private Action<int, int, int[]> _select;
    private Action<bool, int> _cont;

    public UIDelegate(IUserInput userInput)
    {
        _userInput = userInput;
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
        _userInput.RequestMultipleChoices(ToChoices(installSteps[currentStep].optionalFileGroups))
            .ContinueWith<Tuple<int, IEnumerable<int>>?>(choiceDict =>
            {
                var kv = choiceDict.Result;
                if (kv == null)
                {
                    _cont(true, currentStep);
                }
                else
                {
                    _select(currentStep, kv.Item1, kv.Item2.ToArray());
                }
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

    private IEnumerable<Option<int>> ToOptions(Option[] options)
    {
        return options.Select(option => new Option<int>
        {
            Id = option.id,
            Name = option.name,
            Description = option.description,
            ImageUrl = (AssetUrl)option.image,
            Type = MakeOptionState(option),
        }).ToArray();
    }

    private OptionState MakeOptionState(Option option)
    {
        var state = OptionState.Available;
        switch (option.type)
        {
            case "Required": state = OptionState.Required; break;
            case "NotUsable": state = OptionState.Disabled; break;
            case "Recommended": state = OptionState.Selected; break;
        }

        if ((state == OptionState.Available) && option.selected)
        {
            state = OptionState.Selected;
        }

        return state;
    }

    private ChoiceType ConvertChoiceType(string input)
    {
        switch (input)
        {
            case "SelectAtLeastOne": return ChoiceType.AtLeastOne;
            case "SelectAtMostOne": return ChoiceType.AtMostOne;
            case "SelectExactlyOne": return ChoiceType.ExactlyOne;
            default: return ChoiceType.Any;
        }
    }
}

public class InstallerDelegates : ICoreDelegates
{
    public IPluginDelegates plugin => throw new NotImplementedException();

    public IContextDelegates context => throw new NotImplementedException();

    public IIniDelegates ini => throw new NotImplementedException();

    public IUIDelegates ui { get; init; }

    private IUserInput _inputHandler;

    public InstallerDelegates(IUserInput userInput)
    {
        ui = new UIDelegate(userInput);
    }
}
