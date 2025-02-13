using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Settings;

public partial class SettingsView : ReactiveUserControl<ISettingsPageViewModel>
{
    public SettingsView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.BindCommand(ViewModel, viewModel => viewModel.SaveCommand, view => view.SaveButton)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,viewModel => viewModel.CancelCommand,view => view.CancelButton)
                    .DisposeWith(d);

                // this.BindCommand(ViewModel,viewModel => viewModel.CloseCommand,view => view.CloseButton)
                //     .DisposeWith(d);

                this.WhenAnyValue(
                    view => view.ViewModel!.SettingEntries,
                    view => view.ViewModel!.Sections
                ).Select(tuple =>
                {
                    var (entries, sections) = tuple;

                    var dict = sections.ToDictionary(x => x.Descriptor.Id);
                    var grouped = entries
                        .OrderBy(x => x.PropertyUIDescriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
                        .GroupBy(x => x.PropertyUIDescriptor.SectionId)
                        .OrderByDescending(x => dict[x.Key].Descriptor.Priority);

                    var res = new List<IViewModelInterface>();

                    foreach (var group in grouped)
                    {
                        res.Add(dict[group.Key]);
                        res.AddRange(group);
                    }

                    return res;
                }).BindToView(this, view => view.SettingEntriesItemsControl.ItemsSource).DisposeWith(d);
            }
        );
    }
}

