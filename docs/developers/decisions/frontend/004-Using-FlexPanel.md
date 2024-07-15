# Using FlexBox library for control layout:

## Context and Problem Statement
WPF, and sadly Avalonia, are missing a very important panel type: a flex box container.
See https://css-tricks.com/snippets/css/a-guide-to-flexbox/ for details on how this is used to style components in CSS.

Figma, the design tool we are using, uses FlexBox for all of its lay-outing. 
Being able to efficiently replicate the behavior in our application as designed in Figma is the driver for this decision.



`FlexPanel` from [Avalonia.Labs](https://github.com/AvaloniaUI/Avalonia.Labs) was chosen for the task as the most up-to-date version of the third party libraries available and the most likely to get future updates and eventually be integrated into Avalonia project proper.

### Cons:
- Implementation is not feature complete, though most of the core features work as expected
    - See https://github.com/AvaloniaUI/Avalonia.Labs/pull/45 for details of what is supported and not
- In code documentation is basically non-existent requiring developers to learn semantics of `FlexBox` from outside sources (e.g. [FlexBox Specification](https://www.w3.org/TR/css-flexbox-1/#overview))
    - #1754
- ~Some of the default initial values of properties don't match the FlexBox standard.~
    - PR: https://github.com/AvaloniaUI/Avalonia.Labs/pull/74
- FlexPanel doesn't work properly if top level child items have non default Horizontal or Vertical Alignment set (anything other than `Stretch`).
    - Notably, Buttons have Left alignment set in Avalonia.Fluent style.
    - This can also be worked around with a Style setting FlexPanel children alignment to stretch by default.
- Avalonia.Labs is a repository for non API stable controls and panels, breaking changes could be introduced requiring en masse changes from our side to update.
- There is no guarantee that Avalonia.Lab FlexPanel will see further updates that don't come from our team, it only saw two changes since initial commit.

### Pros:
- Much easier time emulating Figma designs since it uses FlexBox everywhere.
- Currently, to emulate the behavior a mix of Grid, StackPanel and WrapPanel are needed and even then it is not always possible to get the same behavior if conditions change (resizing).
- Overcomes many of the limitations of Grid when it comes to distributing items while keeping spacing consistent.
- Single Panel instead of 3
- Entire behavior and contents arrangements be defined form Styles without requiring changes to the actual View
- Possible to define Grow, Shrink, SelfAlignment and base size of child elements without even knowing the type of control of the child elements (could be heterogeneous).
- Should eventually become part of Avalonia proper
- Faster Design implementation times
- Better developer experience.


### Conclusions:
We are going to use `Avalonia.Labs` `FlexPanel` going forwards.
Main reason is the increased productivity and ease of use for developers replicating Figma designs.


### Appendix:
Define properties for children in Styles:
```xml
    <!-- FlexPanel Styles -->
    <Style Selector="panels|FlexPanel">
        
        <Style Selector="^ > :is(Control)">
            <Setter Property="HorizontalAlignment" Value="Stretch" />
            <Setter Property="VerticalAlignment" Value="Stretch" />
            <Setter Property="panels:Flex.Grow" Value="1" />
        </Style>
    </Style>
```
It is possible to reference generic children using `^ > :is(Control)`. the `:is()` syntax is necessary because targeting `Control` directly would not actually work for subclasses that don't explicitly inherit the styling from `Control`.
