# UI Styling / Theming Conventions and Guidelines

## Introduction
This document contains guidelines and tutorials for Stying and Theming the UI of the application.

The design team defines the visual appearance of the application in using Figma designs. These design can evolve and change over time,
requiring the UI of the app to be updated accordingly.

The NexusMods App uses [Avalonia](https://avaloniaui.net/) for its UI framework.
Avalonia has a [CSS-like styling system](https://docs.avaloniaui.net/docs/get-started/wpf/styling), which is used to style the UI.

## Avalonia Styling
The styling system implements cascading styles by searching the logical tree upwards from a control, during the selection step. This means styles defined at the highest level of the application (the App.axaml file) can be used anywhere in an application, but may still be overridden closer to a control (for example in a window, or user control).

When a match is located by the selection step, then the matched control's properties are altered according to the setters in the style.

The XAML for a style has two parts: a selector attribute, and one or more setter elements.
The selector value contains a string that uses the Avalonia UI style selector syntax.
Each setter element identifies the property that will be changed by name, and the new value that will be substituted.
The pattern is like this:

```xml
<Style Selector="selector syntax">
     <Setter Property="property name" Value="new value"/>
     ...
</Style>
```
The Avalonia UI style selector syntax is analogous to that used by CSS (cascading style sheets).

### Example

This is an example of how a style is written and applied to a `Control` element, with a style class to help selection:

```xml
<Style Selector="TextBlock.H1">
    <Setter Property="FontSize" Value="24"/>
    <Setter Property="FontWeight" Value="Bold"/>
</Style>
```
```xml
<Window>
    <StackPanel Margin="20">
        <TextBlock Classes="H1" Text="Heading 1"/>
    </StackPanel>
</Window>
```

### Style locations

Styles can be placed inside a `Styles` collection element on a `Control`, or on the `Application`.
The location of the Styles determines the scope of visibility of the styles that it contains.
If a style is added to the `Application` then it will apply globally.

Styles can also be defined in separate XAML files, and then imported into the `Application`, e.g.:
```xml
<!-- TextBlockStyles.xaml -->
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Style Selector="TextBlock.h1">
        <Setter Property="FontSize" Value="24"/>
        <Setter Property="FontWeight" Value="Bold"/>
    </Style>
</Styles>
```
```xml
<!-- App.axaml -->
<Application... >
    <Application.Styles>
        <FluentTheme/>
        <StyleInclude Source="/TextBlockStyles.axaml"/>
    </Application.Styles>
</Application>
```
You can also include styles from a another assembly by using the `avares://` prefix:
```xml
<Application... >
    <Application.Styles>
        <FluentTheme/>
        <StyleInclude Source="avares://MyApp.Shared/Styles/CommonAppStyles.axaml"/>
    </Application.Styles>
</Application>
```

### Pseudoclass selectors and ContentPresenter weirdness
When styling a control, you may notice that setting some properties using pseudoclasses doesn't work as expected, e.g.:
```xml
<Style Selector="Button">
    <Setter Property="Background" Value="Red" />
</Style>
<Style Selector="Button:pointerover">
    <Setter Property="Background" Value="Blue" />
</Style>
```
You might expect the Button to be red by default and blue when pointer is over it. In fact, only setter of first style will be applied, and second one will be ignored.

The reason is hidden in the Button's template as defined in the default Avalonia Fluent Theme (simplified):
```xml
<Style Selector="Button">
    <Setter Property="Background" Value="{DynamicResource ButtonBackground}"/>
    <Setter Property="Template">
        <ControlTemplate>
            <ContentPresenter Name="PART_ContentPresenter"
                              Background="{TemplateBinding Background}"
                              Content="{TemplateBinding Content}"/>
        </ControlTemplate>
    </Setter>
</Style>
<Style Selector="Button:pointerover /template/ ContentPresenter#PART_ContentPresenter">
    <Setter Property="Background" Value="{DynamicResource ButtonBackgroundPointerOver}" />
</Style>
```
The actual background is rendered by a `ContentPresenter`, which in the default is bound to the Buttons `Background` property.
However in the pointer-over state the selector is directly applying the background to the `ContentPresenter` (`Button:pointerover /template/ ContentPresenter#PART_ContentPresenter`).
That's why when our setter was ignored in the previous code example. The corrected code should target content presenter directly as well:
```xml
<!-- Here #PART_ContentPresenter name selector is not necessary, but was added to have more specific style -->
<Style Selector="Button:pointerover /template/ ContentPresenter#PART_ContentPresenter">
    <Setter Property="Background" Value="Blue" />
</Style>
```

### Style Classes composition
A control can have multiple style classes applied to it, this allows for composition of styles.
```xml
<Style Selector="Border.Rounded">
    <Setter Property="CornerRadius" Value="8" />
</Style>

<Style Selector="Border.OutlineModerate">
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="BorderBrush" Value="{StaticResource ElementStrokeTranslucentModerateBrush}" />
</Style>

<Style Selector="Border.Mid">
    <Setter Property="Background" Value="{StaticResource ElementBackgroundNeutralMidBrush}" />
</Style>
```

```xml
<Border Classes="Rounded Mid OutlineModerate">
    <TextBlock Text="Hello World!"/>
</Boder>
```

This should be used to avoid defining many different combinations of appearances. It can also be used for a sort of inheritance of sorts e.g.:
```xml
<!-- Base Standard Button (only use with additional qualifiers)-->
    <Style Selector="Button.Standard">
        <Setter Property="CornerRadius" Value="4" />
        <Setter Property="Height" Value="36" />
    </Style>


    <!-- Standard Primary -->
    <Style Selector="Button.Standard.Primary">
        <Setter Property="Background" Value="{DynamicResource ElementForegroundPrimaryModerateBrush}" />

        <Style Selector="^:pointerover /template/ ContentPresenter#PART_ContentPresenter">
            <Setter Property="Background" Value="{DynamicResource ElementForegroundPrimaryStrongBrush}" />
        </Style>
    </Style>

    <!-- Standard Primary -->
    <Style Selector="Button.Standard.Primary">
        <Setter Property="Background" Value="{DynamicResource ElementForegroundPrimaryModerateBrush}" />

        <Style Selector="^:pointerover /template/ ContentPresenter#PART_ContentPresenter">
            <Setter Property="Background" Value="{DynamicResource ElementForegroundPrimaryStrongBrush}" />
        </Style>
    </Style>

    <!-- Standard Secondary -->
    <Style Selector="Button.Standard.Primary">
        <Setter Property="Background" Value="{DynamicResource ElementForegroundPrimaryStrongBrush}" />

        <Style Selector="^:pointerover /template/ ContentPresenter#PART_ContentPresenter">
            <Setter Property="Background" Value="{DynamicResource ElementForegroundPrimaryWeakBrush}" />
        </Style>
    </Style>
```

## Avalonia ControlThemes
In addition to Styles, Avalonia also supports WPF like [ControlThemes](https://docs.avaloniaui.net/docs/basics/user-interface/styling/control-themes).
ControlThemes are resources used to define the default appearance of a control, and can be overridden by Styles.

As they are resources, they can have a key and be assigned to a control using the `Theme` property.

ControlThemes also support single inheritance using the `BasedOn` property.

#### Example
```xml
<ControlTheme x:Key="EllipseButton" TargetType="Button" BasedOd="{DynamicResource {x:Type Button}">
    <Setter Property="Background" Value="Blue"/>
    <Setter Property="Foreground" Value="Yellow"/>
    <Setter Property="Padding" Value="8"/>
    <Setter Property="Template">
        <ControlTemplate>
            <Panel>
                <Ellipse Fill="{TemplateBinding Background}"
                         HorizontalAlignment="Stretch"
                         VerticalAlignment="Stretch"/>
                <ContentPresenter x:Name="PART_ContentPresenter"
                                  Content="{TemplateBinding Content}"
                                  Margin="{TemplateBinding Padding}"/>
            </Panel>
        </ControlTemplate>
    </Setter>
</ControlTheme>
```
The above example defines a ControlTheme for a Button, which is based on the default Button ControlTheme (defined in the Avalonia Fluent theme).


### Styling the App
In the App, all the Styles are defined in a dedicated Theme project `NexusMods.Themes.NexusFluentDark`.
This is so that all the styles can be easily found and modified in one place.

### Avalonia Fluent Theme
The theme project makes use of the Avalonia Fluent Theme as a base, which provides a default look for all the Avalonia core controls, using ControlThemes.
For non core controls, such as DataGrid or TreeDataGrid, there are additional themes imported to provide a default look.

The App is able to customize the default look provided by the Fluent Theme by passing a custom `FluentTheme.Palette` of colours.
This is done in the main `NexusFluentDarkTheme.axaml` file, which is the entry point for the Theme project.
The palette determines the default colours for Foreground, Background, Accent, etc...
These cascade down to all the controls, meaning that Foreground and Background colours don't need to be set explicitly unless they change from the default.

`NexusFluentDarkTheme.axaml` is Styles file and is imported into the main application inside `App.axaml`,

### Styles
The Theme uses mainly Avalonia `Styles` rather than `ControlThemes`, for reasons detailed in [Stylying Approach ADR](decisions/frontend/0003-UI-Styling-Approach.md).

Styles should be organized into separate files for each control type, e.g. `TextBlockStyles.axaml`, `ButtonStyles.axaml`, `TextBoxStyles.axaml`, etc.
Some control types may have multiple files, e.g. `StandardButtonStyles.axaml` and `RoundedButtontyles.axaml`.

Each style file needs to be made visible at the top level of `App.axaml`, and this can be done by importing it in the `StylesIndex.axaml` file.

Each Style file should contain a Preview showcasing the various styles in use, e.g.:
```xml
 <Design.PreviewWith>
        <WrapPanel Margin="10" Width="600">
            <Border Classes="Rounded OutlineStrong" Padding="16">
                <TextBlock Text="OUTLINE Strong" />
            </Border>
            <Border Classes="Rounded OutlineModerate" Padding="16">
                <TextBlock Text="OUTLINE Moderate" />
            </Border>
        </WrapPanel>
</Design.PreviewWith>
```

### ControlThemes
`ControlThemes` are only used for base building blocks that need to be referenced in other `Styles`, in particular `TextBlock` typography styles.

`ControlThemes` should be considered for `UserControls`, where the `ContentTemplate` can be defined directly in the `ControlTheme`.
For usage in the App, the `ControlTheme` should either be a default one for all instances of the control, or have a Style class setting the `Theme` property, to avoid a direct reference to the `ControlTheme` in the UI project.

In general, prefer using Styles over ControlThemes, to avoid introducing unnecessary complexity.

### Resources
Resources are used to define colours, brushes, fonts, ControlThemes, and other values such as Opacity, etc.
The use of these resources should be limited to the Theme project, and not used directly in the UI projects, as that would require a reference to the Theme project.

Resources should be placed under the `Resources` folder, and either be placed in a dedicated file, or in the generic `ThemeBaseResources.axaml` file.
`ControlThemes` should be placed in a dedicated file for each control type, under the `Resources/Controls` folder.

Each resource file should be made visible to the rest of the Theme project by importing it in the `ResourcesIndex.axaml` file.
Some exceptions may apply, for example for resources that should not be visible to the entire Theme project but only to the following hierarchy level.

#### Resource Aliases
Resources can be aliased by declaring a new resource with a new Key, referencing the original resource.

Resources can be referenced by either using the `StaticResource` markup extension or the `DynamicResource` one.
`StaticResource` resolves the resource at compile time, while `DynamicResource` resolves it at runtime.

`StaticResource` should be preferred for both minor performance benefits, and compile time checking of the resource existence.
`DynamicResource` should be used when the Value of the resource can change at runtime, which should be rare.

Example:
```xml
<x:Double x:Key="Alpha100">1.00</x:Double>
<x:Double x:Key="Alpha95">0.95</x:Double>
<x:Double x:Key="Alpha90">0.90</x:Double>
<x:Double x:Key="Alpha80">0.80</x:Double>
<x:Double x:Key="Alpha70">0.70</x:Double>
<x:Double x:Key="Alpha60">0.60</x:Double>
<x:Double x:Key="Alpha50">0.50</x:Double>
<x:Double x:Key="Alpha40">0.40</x:Double>
<x:Double x:Key="Alpha30">0.30</x:Double>
<x:Double x:Key="Alpha20">0.20</x:Double>
<x:Double x:Key="Alpha10">0.10</x:Double>
<x:Double x:Key="Alpha5">0.05</x:Double>
<x:Double x:Key="Alpha0">0.00</x:Double>

<!-- Opacity levels -->
<StaticResource x:Key="OpacitySolid" ResourceKey="Alpha100"/>
<StaticResource x:Key="OpacityTransparent" ResourceKey="Alpha0"/>

<StaticResource x:Key="OpacityStrong" ResourceKey="Alpha70"/>
<StaticResource x:Key="OpacityModerate" ResourceKey="Alpha40"/>
<StaticResource x:Key="OpacitySubdued" ResourceKey="Alpha20"/>
<StaticResource x:Key="OpacityWeak" ResourceKey="Alpha10"/>

<!-- Disabled Opacity level -->
<StaticResource x:Key="OpacityDisabledElement" ResourceKey="OpacityModerate"/>
```
In the example above, the `OpacitySolid` resource is an alias for the `Alpha100` resource.
The rest of the Theme should use the `OpacitySolid` resource, and not the `Alpha100`, as the latter is an implementation detail.

### Color System
The app uses a 3 level color system based on the Fluent Design System, with some modifications.
This system was newly introduced to the App with the creation of the Theme project.

The first level represents a palette of primitive colour values (e.g. `Red100`, `Red50`, `Green100`, ...).
To follow is the Brand palette of Semantic colours, (e.g. `BrandWarning90`, `BrandInfo50`, ...).
Finally, the third level is a palette of colors that should be used directly in the Styles of the UI Elements, (e.g. `ElementBackgroundNeutralMidBrush`, `ElementForegroundPrimaryStrongBrush`, ...).

The main idea is that of separating the names of the colours used in the Styles, from the actual values.
This way, if the design team decides to change the colour of all Information elements, this can be accomplished by changing the value of the `ElementInfo` colour, without having to change the name of the colour in all the Styles.

The rest of the Theme project should always prefer to user the third level of the colour system when possible, and the second level only if necessary. Never the first level.

The rest of the resources follow a similar pattern of two or three levels of indirection.

The colours are defined in the `Resources/Palette/Colours` folder, with separate files for each level of the colour system.

Avalonia has both a `Color` type and a `SolidColorBrush` type. Some properties require the former, while other require the latter.
Semantically, a color is just a value, while a brush could have a texture, or a gradient, etc.
In practice, the difference is that `SolidColorBursh` also includes an `Opacity` property, which is not present in `Color`.

The brush version of a color should be preferred when both can be used.

The only place in the entire code where hex color literals should appear is in the `PrimitiveColors.axaml` file, everywhere else colors should be referenced through higher resource aliases.

### Opacity
Like the colors, the App has a system of opacity levels, the primitives are defined in `Resources/Palette/Colors/PrimitiveOpacities.axaml`, while the element layer is defined in `ThemeBaseResources.axaml`.
Developers should avoid using numeric values directly and instead strive to use the semantic aliases instead.

In particular a OpacityDisabledElement alias is defined, which should be used for disabled elements.

### Typography
Similarly to the colour system, the app uses a multilayer typography system for fonts and text styles.

The primitive fonts are defined in the `Resources/Palette/Fonts/PrimitiveFonts.axaml` file, and are then aliased in the `SemanticFonts.axaml` file.

These fonts are then used in the `TextBlockControlThemes.axaml` file, that defines all the Typography styles defined on Figma.
These `ControlThemes` can then be applied by Styles, by setting the `Theme` property of a `TextBlock` to the name of the `ControlTheme`.

This way, a Style for a `Button` can set the Typography of the `TextBlock` inside it, without having to know the details of the Typography styles.

For each `TextBlock` `ControlTheme` an alias Style `Class` is defined in the `TextBlockStyles.axaml` file, to be used in the UI projects.


### Icons
The app primarily uses Material Design Icons (https://pictogrammers.com/library/mdi/).
These need not be manually included in the project, as the App uses the [Icons.Avalonia](https://github.com/Projektanker/Icons.Avalonia) library,
which offers a convenient way to use Material Design Icons in Avalonia.

The way it works is through an `icons|Icon` type, that has a `Value` property that can be set to the mdi-code of the desired icon.
The mdi code can be found by browsing for the icon on a site such as https://materialdesignicons.com/.

The way the app handles icons is by defining a Style `Class` alias that sets the `Value` property of the `Icon` to the desired mdi-code.
The UI projects can then use this Style `Class` to set the icon without having to know the mdi-code.

This way all icons used in the app can easily be found in the `IconsStyles.axaml` file, and the mdi-code can be changed in one place if needed.

### Using Styles in the UI
In general UI projects will change the appearance of controls by setting Style `Classes` on them.

Appearance properties of a control in the UI should NEVER be set on the control themselves, but instead be defined in a separate Style.
Setting appearance properties directly on a control should be avoided because it will override any other styles that are applied to it, effectively making the control appearance unchangeable.
This includes changing appearance of states such as `:pointerover` or `:pressed`.

Developers should NOT define styles outside of the Theme project, e.g. in the UI projects.
Some exceptions may apply, for defining Control properties that determine the Layout of the UI, rather than the appearance.
Follows a list of properties that can be defined directly in the UI projects:
- `Padding`
- `Margin`
- `Spacing`
- `Orientation`
- `Alignment`

Ideally these are defined in the top level `<control>.Styles` collection of the UI file, and not directly on the controls, but this is not a strict requirement.

Some functionality properties of controls can and should be defined directly on the control, e.g. `Command`, `IsEnabled` or `Grid.Row`, `Grid.Column`.

### Naming Conventions
Style Classes should follow PascalCase naming convention.

`ControlThemes` names should follow PascalCase naming convention, and end with `Theme`, e.g. `BodyMDRegularTheme`.

Colors should start with the name of the palette level they belong to, e.g. `ElementBackgroundNeutralMid`.
`SolidColorBrushes` should end with `Brush`, e.g. `ElementBackgroundNeutralMidBrush`.

## Theming
The app doesn't currently support switching themes or loading third party themes.
While there are no current plans for supporting theming, styles have been organized into theme projects to make this potentially easier to implement in the future.
