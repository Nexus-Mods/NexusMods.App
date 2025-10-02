using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Controls.Settings.Section;
using NexusMods.App.UI.Controls.Settings.SettingEntries;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Sdk.Settings;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Icons;
using NexusMods.UI.Sdk.Settings;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Settings;

[UsedImplicitly]
public class SettingsPageViewModel : APageViewModel<ISettingsPageViewModel>, ISettingsPageViewModel
{
    public ReactiveCommand<Unit> SaveCommand { get; }
    public ReactiveCommand<Unit> CancelCommand { get; }
    public ReadOnlyObservableCollection<ISettingEntryViewModel> SettingEntries { get; }
    public ReadOnlyObservableCollection<ISettingSectionViewModel> Sections { get; }
    public R3.ReactiveProperty<bool> HasAnyValueChanged { get; } = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ISettingsManager _settingsManager;

    public SettingsPageViewModel(
        IServiceProvider serviceProvider,
        ISettingsManager settingsManager,
        IWindowManager windowManager,
        IWindowNotificationService notificationService) : base(windowManager)
    {
        _serviceProvider = serviceProvider;
        _settingsManager = settingsManager;

        TabIcon = IconValues.CogOutline;
        TabTitle = Language.SettingsView_Title;

        var settingsConfigs = settingsManager.Configs;
        var propertyConfigs = settingsConfigs.Values
            .SelectMany(settingsConfig => settingsConfig.Properties)
            .Where(propertyConfig => propertyConfig.ContainerOptions is not null)
            .ToArray();

        var sections = serviceProvider.GetServices<SectionDescriptor>().Where(x => !x.Hidden).ToDictionary(x => x.Id, x => x);
        var sectionViewModels = sections.Values.Select(x => new SettingSectionViewModel(x)).ToArray();

        var entryViewModels = propertyConfigs.Where(x => sections.ContainsKey(x.Options.Section)).Select(CreateEntryViewModel).ToArray();

        SettingEntries = new ReadOnlyObservableCollection<ISettingEntryViewModel>(new ObservableCollection<ISettingEntryViewModel>(entryViewModels));
        Sections = new ReadOnlyObservableCollection<ISettingSectionViewModel>(new ObservableCollection<ISettingSectionViewModel>(sectionViewModels));

        SaveCommand = HasAnyValueChanged.ToReactiveCommand(_ =>
        {
            var changedEntries = SettingEntries
                .Where(vm => vm.InteractionControlViewModel.ValueContainer.HasChanged)
                .ToArray();

            if (changedEntries.Length == 0) return;
            foreach (var viewModel in changedEntries)
            {
                viewModel.InteractionControlViewModel.ValueContainer.Update(settingsManager);
            }

            notificationService.ShowToast(Language.ToastNotification_Settings_saved, ToastNotificationVariant.Success);
        });

        CancelCommand = HasAnyValueChanged.ToReactiveCommand(_ =>
        {
            // TODO: ask to discard current values
            foreach (var viewModel in SettingEntries)
            {
                viewModel.InteractionControlViewModel.ValueContainer.ResetToPrevious();
            }
        });

        ViewForMixins.WhenActivated(this, disposables =>
        {
            System.Reactive.Linq.Observable.Merge(SettingEntries
                .Select(vm => vm.WhenAnyValue(x => x.InteractionControlViewModel.ValueContainer.HasChanged)))
                .SubscribeWithErrorLogging(_ =>
                {
                    HasAnyValueChanged.Value = SettingEntries.Any(vm => vm.InteractionControlViewModel.ValueContainer.HasChanged);
                })
                .AddTo(disposables);
        });
    }

    private ISettingEntryViewModel CreateEntryViewModel(PropertyConfig config)
    {
        var containerOptions = config.ContainerOptions;
        Debug.Assert(containerOptions is not null);
        var control = CreateControl(containerOptions, config);

        var markdownRenderer = _serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();
        var linkRenderer = config.Options.HelpLink is null ? null : _serviceProvider.GetRequiredService<IMarkdownRendererViewModel>();
        return new SettingEntryViewModel(config, control, markdownRenderer, linkRenderer);
    }

    private IInteractionControl CreateControl(IContainerOptions options, PropertyConfig config)
    {
        var methods = GetType().GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
        var method = methods.FirstOrDefault(x => x.Name.Equals(nameof(CreateControlImpl)));
        Debug.Assert(method is not null);

        var genericMethod = method.MakeGenericMethod(typeArguments: [options.GetType()]);

        var result = genericMethod.Invoke(obj: null, parameters: [_serviceProvider, options, config, _settingsManager]);
        if (result is not IInteractionControl control) throw new NotSupportedException();

        return control;
    }

    private static IInteractionControl CreateControlImpl<TOptions>(IServiceProvider serviceProvider, TOptions options, PropertyConfig config, ISettingsManager settingsManager)
        where TOptions : IContainerOptions
    {
        var factory = serviceProvider.GetRequiredService<IInteractionControlFactory<TOptions>>();
        return factory.Create(serviceProvider, settingsManager, options, config);
    }
}
