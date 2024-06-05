# UI Styling / Theming Conventions and Guidelines

## Introduction
This document contains guidelines and tutorials for Stying and Theming the UI of the application.

!!! note "The design team defines the visual appearance of the application in using Figma designs. "

    These design can evolve and change over time, requiring the UI of the app to be updated accordingly.

The Nexus Mods App uses [Avalonia](https://avaloniaui.net/) for its UI framework.<br/>
Avalonia has a [CSS-like styling system](https://docs.avaloniaui.net/docs/get-started/wpf/styling), which is used to style the UI.

## Avalonia Styling

!!! info "Avalonia uses a cascading (stacking) style system"

In other words, styles defined at the highest level of the application (the `App.axaml` file) will be be used everywhere
in an application. But, they can still be 'overwritten' closer to a control (for example in a `Window`, or `UserControl`).

When a matching style is located, then the matched control's properties are altered according to the setters in the style.

The XAML for a style has two parts:

- A 'selector' attribute
- One or more setter elements

The selector value contains a string that uses the Avalonia UI style selector syntax.
Each setter element identifies the property that will be changed by name, and the new value that will be substituted.

!!! example

```xml
<Style Selector="selector syntax">
    <Setter Property="property name" Value="new value"/>
         ...
</Style>
```

The Avalonia UI style selector syntax is analogous to that used by CSS (cascading style sheets).

!!! warning "If selectors are not made specific enough, ToolTips and Context menu contents will also be affected"

Selectors should either use the `x:Name`, a Class or the hierarchical path of the control to be styled, 
to avoid affecting other controls of the same type. 

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

!!! info "Styles can be placed inside a `Styles` collection element on a `Control`, or on the `Application`."

The location of the Styles determines the scope of visibility of the styles that it contains.<br/>
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

You can also include styles from a another assembly (i.e. project, NuGet, DLL) by using the `avares://` prefix:

```xml
<Application... >
    <Application.Styles>
        <FluentTheme/>
        <StyleInclude Source="avares://MyApp.Shared/Styles/CommonAppStyles.axaml"/>
    </Application.Styles>
</Application>
```

### Pseudoclass selectors and ContentPresenter weirdness

!!! warning "When styling a control, you may notice that setting some properties using pseudoclasses doesn't work as expected."

```xml
<Style Selector="Button">
    <Setter Property="Background" Value="Red" />
</Style>
<Style Selector="Button:pointerover">
    <Setter Property="Background" Value="Blue" />
</Style>
```
You might expect the Button to be red by default and blue when pointer is over it.<br/>
In fact, only setter of first style will be applied, and second one will be ignored.

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

However in the pointer-over state the selector is directly applying the background to the `ContentPresenter`
(`Button:pointerover /template/ ContentPresenter#PART_ContentPresenter`).

That's why when our setter was ignored in the previous code example.
The corrected code should target content presenter directly as well:

```xml
<!-- Here #PART_ContentPresenter name selector is not necessary, but was added to have more specific style -->
<Style Selector="Button:pointerover /template/ ContentPresenter#PART_ContentPresenter">
    <Setter Property="Background" Value="Blue" />
</Style>
```

### Style Classes Composition

!!! info "A control can have multiple style classes applied to it, this allows for composition of styles."

```xml
<Style Selector="Border.Rounded">
    <Setter Property="CornerRadius" Value="8" />
</Style>

<Style Selector="Border.OutlineModerate">
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="BorderBrush" Value="{StaticResource StrokeTranslucentModerateBrush}" />
</Style>

<Style Selector="Border.Mid">
    <Setter Property="Background" Value="{StaticResource SurfaceMidBrush}" />
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
    <Setter Property="Background" Value="{DynamicResource PrimaryModerateBrush}" />

    <Style Selector="^:pointerover /template/ ContentPresenter#PART_ContentPresenter">
        <Setter Property="Background" Value="{DynamicResource PrimaryStrongBrush}" />
    </Style>
</Style>

<!-- Standard Secondary -->
<Style Selector="Button.Standard.Secondary">
    <Setter Property="Background" Value="{DynamicResource PrimaryStrongBrush}" />

    <Style Selector="^:pointerover /template/ ContentPresenter#PART_ContentPresenter">
        <Setter Property="Background" Value="{DynamicResource PrimaryWeakBrush}" />
    </Style>
</Style>
```

Styles can also be defined nested in parent Styles, e.g.:
```xml
<!-- Base Standard Button (only use with additional qualifiers)-->
<Style Selector="Button.Standard">
    <Setter Property="CornerRadius" Value="4"/>
    <Setter Property="Height" Value="36"/>

    <!-- Standard Primary -->
    <Style Selector="^.Primary">
        <Setter Property="Background" Value="{DynamicResource PrimaryModerateBrush}"/>

        <Style Selector="^:pointerover /template/ ContentPresenter#PART_ContentPresenter">
            <Setter Property="Background" Value="{DynamicResource PrimaryStrongBrush}"/>
        </Style>
    </Style>

    <!-- Standard Secondary -->
    <Style Selector="^.Secondary">
        <Setter Property="Background" Value="{DynamicResource PrimaryStrongBrush}"/>

        <Style Selector="^:pointerover /template/ ContentPresenter#PART_ContentPresenter">
            <Setter Property="Background" Value="{DynamicResource PrimaryWeakBrush}"/>
        </Style>
    </Style>
</Style>
```
This can be useful to keep the code more organized.
Usage of nested definitions should be preferred over defining multiple styles with the same prefix.

## Avalonia ControlThemes

!!! tip "In addition to Styles, Avalonia also supports WPF like [ControlThemes](https://docs.avaloniaui.net/docs/basics/user-interface/styling/control-themes)."

ControlThemes are resources used to define the default appearance of a control, and can be overridden by Styles.

As they are resources, they can have a key and be assigned to a control using the `Theme` property.
ControlThemes also support single inheritance using the `BasedOn` property.

#### Example

```xml
<ControlTheme x:Key="EllipseButton" TargetType="Button" BasedOn="{DynamicResource {x:Type Button}">
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

!!! info "In the App, all the Styles are defined in a dedicated Theme project `NexusMods.Themes.NexusFluentDark`."

This is so that all the styles can be easily found and modified in one place.

The theme project makes use of the Avalonia `Fluent` Theme as a base, which provides a default look for all the
Avalonia core controls, using `ControlThemes`.

!!! note "For non core controls, such as `DataGrid` or `TreeDataGrid`, there are additional themes imported to provide a default look"


### Styling the Avalonia Fluent Theme

!!! info "The App is able to customize the default look provided by the Fluent Theme by passing a custom `FluentTheme.Palette` of colours."

This is done in the main `NexusFluentDarkTheme.axaml` file, which is the entry point for the Theme project.

The palette determines the default colours for Foreground, Background, Accent, etc.

These cascade down to all the controls, meaning that `Foreground` and `Background` colours don't need to be set
explicitly unless they change from the default.

`NexusFluentDarkTheme.axaml` is Styles file and is imported into the main application inside `App.axaml`,

### Styles in Nexus Theme

!!! info "The Theme uses mainly Avalonia `Styles` rather than `ControlThemes`, for reasons detailed in [Styling Approach ADR](../decisions/frontend/0003-UI-Styling-Approach.md)."

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

!!! info "`ControlThemes` are only used for base building blocks that need to be referenced in other `Styles`"

In particular, the `TextBlock` typography styles.

`ControlThemes` should be considered for `UserControls`, where the `ContentTemplate` can be defined directly in the `ControlTheme`.

For usage in the App, the `ControlTheme` should either be a default one for all instances of the control, or have a
`Style` class setting the `Theme` property, to avoid a direct reference to the `ControlTheme` in the UI project.

In general, prefer using Styles over ControlThemes, to avoid introducing unnecessary complexity.

### Resources

!!! info "Resources are used to define colours, brushes, fonts, ControlThemes, and other values such as Opacity, etc."

The use of these resources should be limited to the Theme project, and not used directly in the UI projects,
as that would require a reference to the Theme project.

Resources should be placed under the `Resources` folder, and either be placed in a dedicated file, or in the
generic `ThemeBaseResources.axaml` file. `ControlThemes` should be placed in a dedicated file for each control type,
under the `Resources/Controls` folder.

Each resource file should be made visible to the rest of the Theme project by importing it in the `ResourcesIndex.axaml`
file. Some exceptions may apply, for example for resources that should not be visible to the entire Theme project but
only to the following hierarchy level.

#### Resource Aliases

!!! info "Resources can be aliased by declaring a new resource with a new Key, referencing the original resource."

Resources can be referenced by either using the `StaticResource` markup extension or the `DynamicResource` one.

- `StaticResource` resolves the resource at compile time, while `DynamicResource` resolves it at runtime.
- `StaticResource` should be preferred inside the Theme project for both minor performance benefits, and compile time checking of the resource existence.
- `DynamicResource` should be used when the Value of the resource can change at runtime, which should be rare.
- `DynamicResource` also allow referencing a Theme resource from outside the Theme project without requiring a direct Project dependency.

Occurrences of outside references to resources should be rare, as most resources define appearance properties,
which should be set by Styles, which in turn should be defined in the Theme project.

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

!!! info "The App's Nexus 'Next' theme uses a 3 level color system based on Fluent Design, with some modifications."

This system was newly introduced to the App with the creation of the Theme project.

- The first level is the 'Primitive' later, (e.g. `Red100`, `Red50`, `Green100`, ...).
- The second level is the 'Brand' palette of 'Semantic' colours, (e.g. `BrandWarning90`, `BrandInfo50`, ...).
- The third level is the 'Element' palette of colors that should be used directly in the Styles of the UI Elements,
(e.g. `SurfaceNeutralMidBrush`, `PrimaryStrongBrush`, ...).

!!! tip "The main idea is that of separating the names of the colours used in the Styles, from the actual values."

This way, if the design team decides to change the colour of all `Information` elements, this can be accomplished by
changing the value of the `Info` colour, without having to change the name of the colour in all the Styles.

!!! tip "The Theme project should always prefer to user the third level of the colour system when possible."

And the second level only if necessary. Never the first level.

#### Resources

- The colours are defined in the `Resources/Palette/Colours` folder, with separate files for each level .

!!! note "Avalonia has both a `Color` type and a `SolidColorBrush` type."

    Some properties require the former, while other require the latter.

    Semantically, a color is just a `value`, while a brush could have a `texture`, or a `gradient`, etc.
    In practice, the difference is that `SolidColorBrush` also includes an `Opacity` property, which is
    not present in `Color`.

!!! tip "The brush version of a color should be preferred when both can be used."

The only place in the entire code where hex color literals should appear is in the `PrimitiveColors.axaml` file,
everywhere else colors should be referenced through higher resource aliases.

### Opacity

!!! info "Like the colors, the App has a system of opacity levels."

- The primitives are defined in `Resources/Palette/Colors/PrimitiveOpacities.axaml`.
- The 'element' (third) layer is defined in `ThemeBaseResources.axaml`.

Developers should avoid using numeric values directly and instead strive to use the semantic aliases instead.

!!! tip "In particular a `OpacityDisabledElement` alias is defined, which should be used for disabled elements."

### Other Resource Palettes
!!! info "Like Opacity and Colors, the App has alias palettes for other resources"

Value aliases provide an abstraction over the actual values, and should be used in the Styles of the UI Elements.

#### Spacing
A numbered alias system following the `Spacing-none`, `Spacing-1`, `Spacing-2`, ... pattern.
The values are used to define the `Spacing` property of Panel controls such as `StackPanel`.
The values can be found in `Resources/Palette/Spacing/ElementSpacing.axaml` and 
`/Styles/Controls/StackPanel/StackPanelStyles.axaml` contains classes to apply the spacing to the `StackPanel` control.

#### CornerRadius
`Resources/Palette/CornerRadiuses/ElementCornerRadius.axaml` contains the aliases for the `CornerRadius` property,
of controls such as `Border`. These follow the pattern `Rounded-none`, `Rounded-sm`, `Rounded-md`, ...

To round sides separately use or add an alias `Rounded-{t|r|b|l}{-size}`, e.g `Rounded-t-lg` for a large top rounding.

To round individual corners use or add an alias `Rounded-{tl|tr|bl|br}{-size}`, e.g `Rounded-tl-lg` for a large top-left rounding.

These aliases follow the pattern described on the [Tailwind CSS documentation](https://tailwindcss.com/docs/border-radius).

### Typography

!!! info "There's also a multi-level typography system for fonts and text styles."

The primitive fonts are defined in the `Resources/Palette/Fonts/PrimitiveFonts.axaml` file, and are then aliased in
the `SemanticFonts.axaml` file.

```xml
<!-- Example ControlTheme using SemanticFonts -->
<ControlTheme x:Key="BodyMDNormalTheme" TargetType="TextBlock">
    <!--                       font family set via Semantic Font alias 👇 -->
    <Setter Property="FontFamily" Value="{StaticResource FontBodyRegular}" />
    <Setter Property="FontWeight" Value="Normal" />
    <Setter Property="FontSize" Value="14" />
    <Setter Property="LetterSpacing" Value="0" />
    <Setter Property="LineHeight" Value="21" />
</ControlTheme>
```

!!! info "`TextBlockControlThemes.axaml` defines all the Typography styles defined on Figma"

These `ControlThemes` can then be applied by Styles, by setting the `Theme` property of a `TextBlock` to
the name of the `ControlTheme`.

```xml
<!-- Kind of like this but it's done automatically for you! -->
<TextBlock Theme="{StaticResource BodyMDNormalTheme}" />
```

This way, a Style for a `Button` can set the Typography of the `TextBlock` inside it, without having to know the
details of the Typography styles.

For each `TextBlock` `ControlTheme` an alias Style `Class` is defined in the `TextBlockStyles.axaml` file,
to be used in the UI projects.

!!! example "Example Heading2XLSemi class"

    ```xml
    <Style Selector="TextBlock.Heading2XLSemi">
        <Setter Property="Theme" Value="{StaticResource Heading2XLSemiTheme}" />
    </Style>

    ```

### Icons
!!! info "The app uses a custom `UnifiedIcon` control to display different types of icons."

This control supports `Avalonia.Controls.Image`, `Avalonia.Svg.Skia.Svg`, `Avalonia.Controls.PathIcon` and `Projektanker.Icons.Avalonia.Icon`.
This permits the use of different types of icons interchangeably.

#### Adding & Placing New Icons
!!! info "The app primarily uses [Material Design Icons](https://pictogrammers.com/library/mdi/)."

These need not be manually included in the project, as the App uses the
[Icons.Avalonia](https://github.com/Projektanker/Icons.Avalonia) library, which offers a
convenient way to use Material Design Icons in Avalonia.

To add a material design icon in the App, a new Style `Class` should be defined in the `IconsStyles.axaml` file.
The class should set the `Value` property of the `UnifiedIcon` control to an `IconValue`, 
with the `MdiValueSetter` property set to the mdi-code of the icon.

```xml
<Style Selector="icons|UnifiedIcon.Close">
    <Setter Property="Value">
        <icons:IconValue MdiValueSetter="mdi-close"/>
    </Setter>
</Style>
```

!!! tip "The mdi code can be found by browsing for the icon on a site such as [MaterialDesignIcons](https://materialdesignicons.com/)"

The UI projects can then use this Style `Class` to set the icon without having to know the mdi-code.

```xml
<!--                 👇 -->
<icons:UnifiedIcon Classes="Close" />
```

This way all icons used in the app can easily be found in the `IconsStyles.axaml` file,
and the mdi-code can be changed in one place if needed.

#### Scaling Icons

!!! warning "`UnifiedIcon`s are assumed to be square, and should not have their `Width` and `Height` properties set."

    The `Size` property should be used instead, which will scale the underlying icon to the specified size regardless of type.

`Size="16"` should be equivalent to `Width="16" Height="16"`.

### Using Styles in the UI

!!! tip "UI projects should change the appearance of controls by setting Style `Classes` on them"

Appearance properties of a control in the UI should NEVER be set on the control themselves, but instead be defined in a
separate Style.

Setting appearance properties directly on a control should be avoided because it will override any other styles that
are applied to it, making the control appearance unchangeable.
This includes changing appearance of states such as `:pointerover` or `:pressed`.

!!! warning "Developers should NOT define styles outside of the Theme project, e.g. in the UI projects."

    Some exceptions may apply, for defining Control properties that determine the Layout of the UI, rather than the appearance.

These properties CAN be defined directly in the UI projects:

- `Padding`
- `Margin`
- `Spacing`
- `Orientation`
- `Alignment`

Ideally these are defined in the top level `<control>.Styles` collection of the UI file, and not directly on the
controls, but this is not a strict requirement.

Some functionality properties of controls can and should be defined directly on the control, e.g.
`Command`, `IsEnabled` or `Grid.Row`, `Grid.Column`.

### Naming Conventions

- Style Classes should follow PascalCase naming convention.

- `ControlThemes` names should follow PascalCase naming convention, and end with `Theme`, e.g. `BodyMDRegularTheme`.

- Colors should start with the name of the palette [level](#color-system) they belong to, e.g. `SurfaceMid`.
  `SolidColorBrushes` should end with `Brush`, e.g. `SurfaceMidBrush`.

## 3rd Party Theming

!!! note "The app doesn't currently support switching themes or loading third party themes."

While there are no current plans for supporting theming, styles have been organized into theme
projects to make this potentially easier to implement in the future.
