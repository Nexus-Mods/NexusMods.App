using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.Abstractions.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Settings.SettingEntries;

public class SettingComboBoxViewModel : AViewModel<ISettingComboBoxViewModel>, ISettingComboBoxViewModel
{
    public SingleValueMultipleChoiceContainer ValueContainer { get; }

    public string[] DisplayItems { get; }

    [Reactive] public int SelectedItemIndex { get; set; }

    public SettingComboBoxViewModel(SingleValueMultipleChoiceContainer valueContainer)
    {
        ValueContainer = valueContainer;

        DisplayItems = valueContainer.Values.Select(x => x.Value).ToArray();
        SelectedItemIndex = GetIndex(valueContainer.CurrentValue);

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.SelectedItemIndex)
                .Select(GetValue)
                .BindToVM(this, vm => vm.ValueContainer.CurrentValue)
                .DisposeWith(disposables);
        });
    }

    private object GetValue(int index)
    {
        var values = ValueContainer.Values;
        if (index == -1) return values.First().Key;
        if (index >= values.Length) return values.Last().Key;
        return values[index].Key;
    }

    private int GetIndex(object value)
    {
        for (var i = 0; i < ValueContainer.Values.Length; i++)
        {
            var other = ValueContainer.Values[i];
            if (ValueContainer.EqualityComparer.Equals(other.Key, value))
                return i;
        }

        return -1;
    }
}
