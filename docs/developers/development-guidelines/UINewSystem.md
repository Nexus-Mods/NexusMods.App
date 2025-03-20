# UI Refactor (March 2025)



## Introduction

This document contains guidelines and tutorials for Stying and Templating the UI of the application.

Depending on what the control will look like or what functionality is required will determine what method is used for styling. I considering this a type of flow chart for how complex we want to go.

### Styling

The most basic of visual changes when functionality stays the same. Styling is when the look of a control changes without changing any underlying markup and structure. Primarily used for color and font changes for various states but is used for layout changes such as margin, padding, alignment, spacing etc.

For example, if we want to style an Avalonia `Seperator` control to match our system, we would create a new `SeperatorStyles.axaml` file inside of `src/Themes/NexusMods.Themes.NexusFluentDark/Styles/Controls/Separator`. 

Any created style files also need referencing in the `src/Themes/NexusMods.Themes.NexusFluentDark/Styles/StylesIndex.axaml` file.

!!! info Note:

    - Create a Design View for quick iteration.
    - Use `StaticResource`'s from our design system where possible.

### ControlTheme

If we need more elements to style than have been provided by Avalonia's default ControlTheme, we will need to provide a new one. 

For example, if we want to change the structure of a `Button` control to have a `Border` around it, we would need to create a new `ControlTheme` for the `Button` control.

Copy the default `Button` control theme from the Avalonia repository into `src/Themes/NexusMods.Themes.NexusFluentDark/Controls/Button.axaml` and modify it to suit our needs.

Styles and classes can be added as part of the `ControlTheme` or alternatively as a separate `Style` file. This will be determined by the complexity of the control and how many controls will use the same styles. 

Any created `ControlTheme`'s also need referencing in the `src/Themes/NexusMods.Themes.NexusFluentDark/Resources/ResourceIndex.axaml` file.

!!! info Note:

    - Try the Avalonia default ControlTheme first before creating a new one.
    - Try to follow Avalonia's style and naming conventions where possible as this will make it easier for other developers to understand and extend.
    - Use `StaticResource`'s from our design system where possible.
    - Be aware of the controls `TemplateBinding`'s and how they are used. 
    - Be mindful of the control's functionality when changing it's structure, you may need to create a new control (see below).
    - Always aim to create a Design View for quick iteration.

### Functionality

If we need to change the behavior of a control, we will need to create a new control and inherit from the control we want to change.

This can be as simple as adding a new property to a control or as complex as creating a new control from scratch.

This new control will be paired with a new ControlTheme to complete the control.

## Resources

Avalonia uses keyed resources to style controls. This allows to define a style in one place and use it in multiple places.

Our styling is based on Avalonia's Fluent theme and inherits from it. This means we can use the same resources as the Fluent theme and override them where necessary. This is done by creating resources witham the se key as the Avalonia theme. We primarily use `StaticResource`'s to reference these resources as it's more performant than `DynamicResource`'s. Our Resources can be found across various files within the `src/Themes/NexusMods.Themes.NexusFluentDark/Resources` folder, some are static values from our Figma design system and others are abstracted values that can be reused across the application. Our resources that override the Fluent theme are mainly in the `BaseResources.axaml` and `ControlResources.axaml` files.

All of our Resources and Styles are then merged into our theme within the `src/Themes/NexusMods.Themes.NexusFluentDark/NexusFluentDarkTheme.axaml` file.

## Figma to Avalonia Workflow

Thingss to consider when converting Figma designs to Avalonia:

- Colors
- Layout
- What controls and when
- Not everything is a StackPanel
- Grid or DockPanel
- MaxWidth weirdness and the Grid workaround

## Links


## Resources

- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [Avalonia GitHub](https://www.github.com/AvaloniaUI/Avalonia)


