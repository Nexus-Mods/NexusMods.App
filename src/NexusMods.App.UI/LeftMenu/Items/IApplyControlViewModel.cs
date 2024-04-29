﻿using System.Reactive;
using NexusMods.App.UI.Controls.Navigation;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface IApplyControlViewModel : IViewModelInterface
{
    ReactiveCommand<Unit,Unit> ApplyCommand { get; }
    
    ReactiveCommand<Unit,Unit> IngestCommand { get; }
    
    ReactiveCommand<NavigationInformation, Unit> ShowApplyDiffCommand { get; }
    
    ILaunchButtonViewModel LaunchButtonViewModel { get; }
    
    string ApplyButtonText { get; }
}
