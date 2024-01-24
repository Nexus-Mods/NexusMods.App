# UI Styling and Theming approach

## Context and Problem Statement

We need a standardized way to easily style and theme our UI components.
Ideally it should be easy maintain and change/update in the future.

Avalonia has a built-in CSS like styling system based on selectors and Classes, as well as more WPF like theming system
based on ControlThemes.
ControlThemes are a recent addition and the documentation doesn't indicate a preferred approach for styling an entire
application.

This document aims to identify the best solution for styling and theming our application.

## Decision Drivers

1. Finding the Styles/Themes affecting a control should be easy.
2. It should be clear where new styles/themes should be placed.
3. Styling should be reused as much as possible, avoiding code duplication.
4. It should be easy to style new UIs and controls.
5. It should be easy to change the styling of existing UIs and controls.
6. It should be possible to overrule the Theming of a specific control from code (e.g. setting text to red for invalid
   input).
7. It should be possible to change the appearance of any UI element from the Theme project.
7. Ability to inherit from existing styles/themes (avoid code duplication).
8. Ability to not inherit from existing styles/themes (not be affected by other styles).9.- Ability to change Styles of
   child elements from parent elements.
9. Support for custom Themes.
10. Ability to inherit from existing Themes.
11. Ability to not inherit from existing Themes (not be affected styles from other Themes).
12. Changing Themes at runtime (nice to have).
13. Avoiding circular dependencies between UI and Theme projects.

## Examined Options

The obvious solution for 1., 2., 3. and 5. is to have all the styles and themes in a centralized location, so separate
from the UIs and controls.
This would make it easy to find the styles, change them and add new ones.

If they were scattered throughout the application, changing them would require searching through the entire application.

Consolidating all the themes to a single location would also make it simpler to switch out all the themes for something
else
(e.g. changing a single import statement, changes the entire theme of the application).

Regarding how the Styles/Themes should be defined, the following options are considered:

### Option 1: Use Styles

[Docs](https://docs.avaloniaui.net/docs/basics/user-interface/styling/styles)
Styles can be defined directly on a control or in dedicated Style files, which are not resource dictionaries.
Styles can have internal ResourceDictionaries that are scoped to the style itself.

Pros:

- It is possible to apply multiple styles to a control, permitting composition of styles.
- All styles applied to a control are combined, allowing for a form of implicit inheritance.
- Styles can set the styling of child elements.
- Familiar for current developers.
- Similarity with CSS makes it easier to implement from Figma designs.
- Can be overridden from code for special cases (e.g. setting text to red for invalid input).
- Classes are strings, allowing UI projects to use them without requiring a reference to the Theme project.
- It is possible to create a style for a user control without having a reference to the type.
  - This requires changing the `StyleKeyOverride` property of the control to to a type that is accessible from the Style definition.
  - Then the style would select it using the StyleKey type and either the name or a class.

Cons:

- Adding a style to a control doesn't clear other applied styles, potentially leading to controls with unwanted styles
  being applied.
    - To solve cases of this, the new style needs explicitly override all the values set by other styles.
    - Could lead to a lot of boilerplate code just to disable existing styling.
- It's not possible for a Style definition to inherit from another Style definition.
    - This means that if a Style wants to reuse the definition of an existing style, it needs to copy all the values
      from the existing style.
    - This could lead to a lot of code duplication.
- Nothing prevents developers from defining styles outside the centralized location and these would take precedence over
  the centralized styles.
    - Unless caught during review, this could lead to scattered styles and hard to debug styling issues.
- Style classes are simple strings, there is no compile time checking that used classes actually exist.
    - There is actually Syntax highlighting when using classes, but only if the class is defined in the same file.
    - Vulnerable to typos.
    - Refactoring changes to class names are not automatically propagated to users of the class, leaving unstyled
      controls.
    - There is no Go to Definition support for style classes.
- While it is possible to change the styling of child controls, it isn't possible to apply a style class to a child control.
  - This means that there is no way to reuse the already defined styles for child controls.

### Option 2: Use ControlThemes

[Docs](https://docs.avaloniaui.net/docs/basics/user-interface/styling/control-themes)
These are similar to WPF's Styles, and their main purpose appears to be that of defining the default appearance of a
control.

They are defined in a ResourceDictionary and can be applied to a control either explicitly using the `Theme` property or
implicitly to all controls of a specific type by setting `x:Key="{x:Type Button}"`.


Pros:

- Only a single ControlTheme can be applied to a control, this provides isolation, no carry over from other themes.
- ControlThemes support single inheritance, through `BasedOn` property, allowing for code reuse.
    - Isolation and optional inheritance allows the developers to choose what to carry over from other themes.
- By setting a ControlTheme as the default for a control type, it is possible to for UI projects to not require a reference to the Theme project.
  - To support variants (e.g. button variants), the ControlTheme can contain Class definitions that can then be used by UI projects.
  - Using classes inside the ControlTheme allows for composition of styles.
- ControlThemes support ThemeVariants, allowing for different version of Resources to be defined for different Variants.
  - Theme Variants are intended for light/dark themes, but could potentially be leveraged for a more supporting multiple Theme support.

Cons:

- Only one ControlTheme can be applied to a control.
    - There is no way to combine multiple ControlThemes onto a single control, requiring duplicating the contents of one
      of the two into a new one.
- ControlThemes can only style the control itself plus anything defined in the ContentTemplate.
    - Normal child elements can't be styled from the ControlTheme.
    - Elements defined in the ContentTemplate become part of the control then, meaning you can't change the Class of an
      icon defined in the ContentTemplate.
    - Controls with ContentTemplates become less flexible or they require subclassing the control to be able to add new
      properties for template elements.
    - This is very limiting as for example a button variant can't set the colors of the icon inside it, which is
      possible with Styles.
- ControlThemes require a reference to the Type of the control they are styling for the `TargetType` property.
    - This means that the Theme project needs to reference the projects containing controls to style.

Considering the above, the best approach would seem to be to use a single ControlTheme for each control type,
and declaring Style classes inside it to allow for variants and composition. This does sacrifice the ability to use inheritance though.

### Option 3: Use Styles on top of ControlThemes
The idea would be to use ControlThemes to define base building blocks of the Styling,
then have Style classes that apply the ControlThemes to specific controls.

Pros:
- Styles can apply to child elements of a control.
- Styles can set the `Theme` property of a control, allowing to apply a ControlTheme to a control from a Style.
  - This allows for Styles to "inherit" from a ControlTheme, which in turn can inherit from other ControlThemes.
  - Styles can be composed (Only one Theme will be applied in the end though!).
  - Styles can set the `Theme` property of a child control, allowing to theme child elements without code duplication.
- Style Classes can be applied to a control without requiring a reference to the Theme project.
- Ability to style child elements is crucial to allow us to customize the appearance of UserControls.

Cons:
- Styles can't be isolated using ThemeVariants, making ThemeVariants not usable for switching between Themes.
- Using both Styles and ControlThemes could be confusing for developers.
  - The Theme project would primarily use ControlThemes, while the UI projects would need to use Style Classes.

## Decision Outcome

It was decided to use primarily Styles, with some ControlThemes for base building blocks.
Particularly, ControlThemes are only used for `TextBlock` fonts, as this allows Styles for other elements (e.g. Buttons)
to apply the font by setting the `Theme` property.

### Consequences
- UI projects don't need to reference the Theme project.
- Styles can be composed in the UI projects, allowing for simpler style definitions (not having to define all combination explicitly).
- Usage of ControlThemes was limited to reduce complexity of the approach.

Main drawbacks are:
- Styles can't be isolated using ThemeVariants, making ThemeVariants not usable for switching between Themes.
- Styles can't set styles for child elements, but there is an open issue to add support for this (https://github.com/AvaloniaUI/Avalonia/issues/12964)
- Since UI shouldn't have reference of the Theme project, that means that UI projects can't access any resource from the Theme project,
including colors and other values. Classes need to be used instead, which makes some things more verbose.


