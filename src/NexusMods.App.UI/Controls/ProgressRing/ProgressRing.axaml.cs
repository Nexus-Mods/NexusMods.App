﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NexusMods.App.UI.Controls;

public partial class ProgressRing : UserControl
{
    public ProgressRing()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

