# Workspaces

## Components

The workspace system is made up of the following components:

- Workspaces
- Panels
- Tabs
- Pages

```mermaid
flowchart TD
    subgraph Window 2
        Workspace2[Workspace 2]
        Workspace2 --> Panel3[Panel 3]
        Panel3 --> Tab4[Tab 4] --> Page
    end

    subgraph Window 1
        Workspace1[Workspace 1]
        Workspace1 --> Panel1[Panel 1]
        Workspace1 --> Panel2[Panel 2]
        Panel1 --> Tab1[Tab 1] --> Page
        Panel1 --> Tab2[Tab 2] --> Page
        Panel2 --> Tab3[Tab 3] --> Page
    end
```

- Each window can only contain **a single** workspace.
- Each workspace can contain **one or more** panels.
- Each panel can contain **one or more** tabs.
- Each tab contains **a single** page, which are the contents to be displayed.

Starting with the smallest component, a tab is comparable to a browser tab. Tabs own the content to display and clicking
on another tab will change the displayed content of the current panel. The contents of a tab can only be a page, which
is a generalized concept to hold any view.

Panels contains the tabs and are responsible for displaying the currently selected tab, and a "tab strip" for selecting
other tabs. Closing a panel will automatically close all of its tabs.

The workspace primarily deals with the panel layout, sizes and position of panels.

## Panels

- A panel always takes up the maximum amount of available space.
- A panel has logical and actual bounds.
    - Logical bounds describe the ratio of space the panel takes up inside the workspace.
        - Example 1: `X: 0, Y: 0, Width: 1, Height: 1` is a panel that is positioned in the top left corner and takes up
          100% of the width and height of the workspace.
        - Example 2: `X: 0.5, Y: 0, Width: 0.5, Height: 1` describe a column on the right side, it takes up 100% of the
          height but only 50% of the width of the workspace and is positioned on the right.
    - Actual bounds are the exact pixel sizes of the panel on the monitor. They are derived by multiplying the logical
      bounds with the pixel size of the workspace.

### Adding a Panel

- Additional panels can only be added to the workspace if the maximum number of columns and rows aren't reached yet.
- An existing panel must be split in half to create space for a new panel.
- An existing panel can be split into two columns if the number of columns in the current row isn't greater than the
  maximum amount of allowed columns.
- An existing panel can be split into two rows if the number of rows in the current column isn't greater than the
  maximum amount of allowed rows.

### Closing a Panel

- Closing a panel is done by joining the current panel with **one or more** adjacent panels.
    - An adjacent panel is a panel that is in the same row or column as the current panel and share a border.
- Columns are preferred to rows when the workspace is horizontal (width > height).
- Rows are preferred to columns when the workspace is vertical (height > width).
- The height of the current panel will be added to the panels in the same column.
- The width of the current panel will be added to the panels in the same row.

## Persistence and Factories

Workspaces will be saved to our existing data store by transforming the data into a JSON serializable data structure.

This can be found at [`WorkspaceData.cs`](https://github.com/Nexus-Mods/NexusMods.App/blob/main/src/NexusMods.App.UI/WorkspaceSystem/DataStore/WorkspaceData.cs).
Besides properties like the logical bounds of a panel, the contents of the tabs have to be serialized as well. This is
done using a factory approach:

A page can be constructed using an implementation
of [`IPageFactory`](https://github.com/Nexus-Mods/NexusMods.App/blob/main/src/NexusMods.App.UI/WorkspaceSystem/Page/IPageFactory.cs). Each factory has an identifier
that must be globally unique, and a method that takes in a "parameter" to produce a page. The provided generic abstract
class should be used instead of the non-generic interface, if you want to implement this interface. The parameter
contains the data required to construct the page, which will be an ID in most cases.

Do note that the parameter type has to be registered in the [`TypeFinder`](https://github.com/Nexus-Mods/NexusMods.App/blob/main/src/NexusMods.App.UI/TypeFinder.cs)
and the factory has to be registered with DI in [`Services`](https://github.com/Nexus-Mods/NexusMods.App/blob/main/src/NexusMods.App.UI/Services.cs#L60).

