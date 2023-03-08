using ICSharpCode.SharpZipLib.Lzw;
using Microsoft.Extensions.Logging;
using NexusMods.CLI.DataOutputs;
using NexusMods.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexusMods.CLI;

public class CLIInput : IUserInput
{
    public ILogger<CLIInput> _logger;
    public IRenderer _renderer;

    private static string ReturnInput = "x";

    public CLIInput(ILogger<CLIInput> logger, Configurator configurator)
    {
        _logger = logger;
        _renderer = configurator.Renderer;
    }

    public Task<IEnumerable<OptionIdT>> RequestChoice<OptionIdT>(string query, ChoiceType type, IEnumerable<Option<OptionIdT>> options)
    {
        var done = false;
        var current = options.ToList();
        while (!done)
        {
            var input = GetUserInput();
            if (input == ReturnInput)
            {
                done = true;
            } else
            {
                var idx = ParseNumericalUserInput(input, current.Count());
                if (idx != null)
                {
                    current[idx ?? 0].Type = ToggleState(current[idx ?? 0].Type);
                }
                _renderer.Render(TableOfOptions(options));
            }
        }
        return Task.FromResult(current.Where(_ => _.Type == OptionState.Selected || _.Type == OptionState.Required).Select(_ => _.Id));
    }

    public Task<Tuple<GroupIdT, IEnumerable<OptionIdT>>?> RequestMultipleChoices<GroupIdT, OptionIdT>(IEnumerable<ChoiceGroup<GroupIdT, OptionIdT>> groups)
    {
        var selectedGroupIdx = -1;

        Tuple<GroupIdT, IEnumerable<OptionIdT>>? result = null;

        IList<Option<OptionIdT>>? selectedGroup = null;

        while (true)
        {
            RenderOptions(groups, selectedGroup);

            var input = GetUserInput();

            if (selectedGroupIdx < 0)
            {
                selectedGroupIdx = ParseNumericalUserInput(input, groups.Count()) ?? selectedGroupIdx;
                selectedGroup = selectedGroupIdx >= 0
                    ? groups.ElementAt(selectedGroupIdx).Options.ToList()
                    : null;
            }
            else
            {
                if (input == ReturnInput)
                {
                    result = CreateResult(groups, selectedGroupIdx, selectedGroup);
                }
                else
                {
                    UpdatedSelectedGroup(ref selectedGroup, groups, selectedGroupIdx, input);
                }
            }

            if (input == ReturnInput)
            {
                break;
            }
        }
        return Task.FromResult(result);
    }

    private string GetUserInput()
    {
        return (Console.ReadLine() ?? "").Trim();
    }

    private void UpdatedSelectedGroup<GroupIdT, OptionIdT>(ref IList<Option<OptionIdT>>? selectedGroup,
                                                           IEnumerable<ChoiceGroup<GroupIdT, OptionIdT>> groups,
                                                           int selectedGroupIdx,
                                                           string input)
    {
        var idx = ParseNumericalUserInput(input, selectedGroup!.Count()) ?? -1;
        if (idx >= 0)
        {
            var oldState = selectedGroup![idx].Type;
            selectedGroup![idx].Type = ToggleState(selectedGroup[idx].Type);
            if (oldState != selectedGroup![idx].Type)
            {
                var groupType = groups.ElementAt(selectedGroupIdx).Type;
                FixSelection(ref selectedGroup!, groupType, idx);
            }
        }
    }

    private static Tuple<GroupIdT, IEnumerable<OptionIdT>> CreateResult<GroupIdT, OptionIdT>(IEnumerable<ChoiceGroup<GroupIdT, OptionIdT>> groups,
                                                                                             int selectedGroupIdx,
                                                                                             IList<Option<OptionIdT>>? selectedGroup)
    {
        var updatedOptions = selectedGroup!
                                .Where(_ => (_.Type == OptionState.Selected) || (_.Type == OptionState.Required))
                                .Select(_ => _.Id);

        return new Tuple<GroupIdT, IEnumerable<OptionIdT>>(groups.ElementAt(selectedGroupIdx).Id, updatedOptions);
    }

    private void FixSelection<OptionIdT>(ref IList<Option<OptionIdT>> list, ChoiceType groupType, int lastChangeIdx)
    {
        if (((groupType == ChoiceType.ExactlyOne) || (groupType == ChoiceType.AtMostOne))
            && (list[lastChangeIdx].Type == OptionState.Selected)) {
            DeselectAllBut(ref list, lastChangeIdx);
            return;
        } 

        if (((groupType == ChoiceType.ExactlyOne) || (groupType == ChoiceType.AtLeastOne))
            && !list.Any(_ => _.Type == OptionState.Selected))
        {
            list[lastChangeIdx].Type = OptionState.Selected;
        }
    }

    private void DeselectAllBut<OptionIdT>(ref IList<Option<OptionIdT>> list, int lastChangeIdx)
    {
        for (var i = 0; i < list.Count; ++i)
        {
            if ((i != lastChangeIdx) && (list[lastChangeIdx].Type == OptionState.Selected))
            {
                list[i].Type = OptionState.Available;
            }
        }
    }

    private int? ParseNumericalUserInput(string input, int upperLimit)
    {
        try
        {
            var idx = int.Parse(input ?? "") - 1;
            if ((idx >= 0) && (idx < upperLimit))
            {
                return idx;
            }
        }
        catch (FormatException)
        {
        }

        // input invalid or out of range
        return null;
    }

    private void RenderOptions<GroupIdT, OptionIdT>(IEnumerable<ChoiceGroup<GroupIdT, OptionIdT>> groups, IList<Option<OptionIdT>>? selectedGroup)
    {
        Table table;
        if (selectedGroup == null)
        {
            table = TableOfGroups(groups);
        }
        else
        {
            table = TableOfOptions(selectedGroup!);
        }
        _renderer.Render(table);
    }

    private Table TableOfOptions<OptionIdT>(IEnumerable<Option<OptionIdT>> current)
    {
        IList<object[]> row = new List<object[]>();
        var key = 1;
        foreach (var item in current)
        {
            row.Add(new object[] { key++, RenderOptionState(item.Type), item.Name, item.Description ?? "" });
        }
        row.Add(new object[] { ReturnInput, "", "Back", "" });

        var headers = new[] { "Key", "State", "Name", "Description" };

        return new Table(headers, row);
    }

    private static Table TableOfGroups<GroupIdT, OptionIdT>(IEnumerable<ChoiceGroup<GroupIdT, OptionIdT>> groups)
    {
        IList<object[]> row = new List<object[]>();
        var headers = new[] { "Key", "Group" };

        var key = 1;
        foreach (var item in groups)
        {
            row.Add(new object[] { key++, item.Query });
        }
        row.Add(new object[] { ReturnInput, "Continue" });

        return new Table(headers, row);
    }

    private OptionState ToggleState(OptionState old)
    {
        switch (old)
        {
            case OptionState.Available: return OptionState.Selected;
            case OptionState.Selected: return OptionState.Available;
            default: return old;
        }
    }

    private string RenderOptionState(OptionState state)
    {
        switch (state)
        {
            case OptionState.Disabled: return "Disabled";
            case OptionState.Available: return "Off";
            case OptionState.Required: return "Required";
            case OptionState.Selected: return "On";
        }
        throw new NotImplementedException();
    }
}
