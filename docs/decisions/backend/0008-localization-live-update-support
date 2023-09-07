```
status: accepted
date: {2023-09-06 when the decision was last updated}
deciders: {App Team @ 2023-09-05 Meeting}
```

# Localization live update support

## Context and Problem Statement

When a user changes the language of the app, the UI needs to be update to reflect the new language.
This could be solved by requesting the user to restart the App.
However, a better user experience would be to update the UI live.

Any UI component that refreshes for other reasons (e.g. new message box) should already show the new language.
However, other UI components are long lived and would not be refreshed, unless measures are taken.

## Considered Options

* Request the user to restart the App
* Make all instances of localizable strings in the UI update when the locale is changed.

## Decision Outcome

Chosen option: "Request the user to restart the App", because
- It's less complex to implement
- It's more robust (compile time safety)
- It's more performant (no need to update all UI components)

<!-- This is an optional element. Feel free to remove. -->
### Consequences

* Good, because:
    * Adding a localizable string is simple and straightforward
    * Contributors don't need to worry about making the UI update when the locale is changed
    * Axaml compiler can check that the localization references exist.
* Bad, because:
    * The user experience is not as good as it could be
    * XAML stylesheet files can't contain static localizable references as they aren't updated when the locale is set on startup.
    * There might be other cases of long lived strings that still need to be updated after startup.
