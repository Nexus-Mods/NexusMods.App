﻿using System.Windows.Input;
using Avalonia.Media;
using NexusMods.App.UI.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons;

public class GameViewModel : SpineButtonViewModel
{
    [Reactive]
    public bool IsActive { get; set; }
    
    [Reactive]
    public string Name { get; set; }
    
    [Reactive]
    public IImage Image { get; set; }
    
    [Reactive]
    public ICommand Click { get; set; }
    
    [Reactive]
    public object Tag { get; set; }
}