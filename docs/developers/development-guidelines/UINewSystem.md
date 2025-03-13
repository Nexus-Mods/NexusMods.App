# UI Refactor (March 2025)

## Introduction

This document contains guidelines and tutorials for Stying and Templating the UI of the application.

Depending on what we want the control to look like or do will determine how deep we should go into styling and theming. I considering this a type of flow chart for how complex we want to go.

### Styling

The most basic of visual changes. Styling is when just change the look of a control without changing any underlying markup and structure. Color changes, font changes, etc.


### ControlTheme

If we need more elements to style than have been provided by Avalonia's default ControlTheme, we will need to provide a new one. 

For example, if we want to change the structure of a `Button` control to have a `Border` around it, we would need to create a new `ControlTheme` for the `Button` control.

Copy the default `Button` control theme from the Avalonia repository and modify it to suit our needs.

### Functionality

If we need to change the behavior of a control, we will need to create a new control and inherit from the control we want to change.

This can be as simple as adding a new property to a control or as complex as creating a new control from scratch.

## Figma to Avalonia Workflow

- Colors
- Layout

Theming is when changing the structure of a control without 

This is going to cover the following topics:
- Figma to Avalonia workflow when styling and theming 
- When to just style, when to set new control theme

