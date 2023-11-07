﻿using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;

[ExcludeFromCodeCoverage]
public partial class FooterView : ReactiveUserControl<IFooterViewModel>
{
    public FooterView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // this.OneWayBind(ViewModel, vm => vm.CanInstall, view => view.InstallButton.IsEnabled)
            //     .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CancelCommand, view => view.CancelButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.InstallCommand, view => view.InstallButton)
                .DisposeWith(disposables);
        });
    }
}
