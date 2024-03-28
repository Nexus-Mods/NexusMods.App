# Diagnostics

The diagnostic system is designed to allow the mod manager to inform the user about issues with their current setup. Conceptually, it's similar to the issues and suggestions you get in your code editor.

## Components

### Id

It's important to understand that this Id refers to the diagnostic type, not to the individual object instance. This is not auto-generated, like a GUID, but instead hardcoded. This is similar to the [Compiler Errors](https://learn.microsoft.com/en-us/dotnet/csharp/misc/cs0027) in .NET where each error type has it's own Id, eg: `CS0027`.

The [`DiagnosticId`](https://github.com/Nexus-Mods/NexusMods.App/blob/main/src/Abstractions/NexusMods.Abstractions.Games.Diagnostics/DiagnosticId.cs) struct contains two fields:

- `Source`, this should be the name of the assembly that produces the diagnostic.
- `Number`, this is a monotonic increasing number of the specific diagnostic type.

You can use a const field for the `Source`:

```csharp
internal static partial Diagnostics
{
    private const string Source = "NexusMods.Example";
}
```

### Title

Similar to `Id`, the Title refers to the diagnostic type, not to the individual object instance. If you have a diagnostic about a broken texture, then the Title could be `Broken Texture` or similar. The Title should describe the type of diagnostic and will be constant for every diagnostic of the same type.

### Severity

Severity levels allow users to gauge the importance of the diagnostic. A diagnostic with a higher severity should be fixed first by the user.

The current system has three Severity levels:

#### Suggestion

Something that doesn't indicate a problem, and offers improvements to the user.

This severity can be used to provide helpful advices to the user. Applying these
suggestions MUST NOT introduce further diagnostics of a higher severity. Furthermore,
analysis and deduction MUST BE objective and based on facts. Suggestions have to
be documented, and their improvements must be justifiable. Don't offer improvements
if you aren't sure that the user benefits from them.

**Examples:**

- After analysis of the installed mods and their files, packing them into an archive, instead of having them as loose files, will improve load times.

#### Warning

Something that has an unintended adverse effect on any part of the game.

This severity encompasses unintended problems that negatively impact the game
in any of its aspects. This includes the visuals, the performance, and even the gameplay.

**Examples:**

- Mods will fail to load due to incompatibilities. This won't crash the game but it's still an unintended adverse effect.
- Missing assets that don't crash the game but manifest themselves as a noticeable abnormality. As an example, missing textures will get replaced with a default duo color checker-pattern, or a single color texture.

#### Critical

Something that will make the game unplayable.

Unplayable, in this context, means that the user is unable
to interact with the game. The most common effect of such
problems is a crash. The frequency or likelihood of such
causes or conditions are irrelevant. A crash that happens
when the user starts the game, and a crash that happens
when the user is in a very specific situation, are equal
in severity.

**Examples:**

- A crash.
- A prolonged or indefinite loading screen.
- A prolonged or indefinite freeze.

#### Choosing the Severity

It's important to choose the right Severity for your diagnostic. The majority of diagnostics will be Warnings, as they are for any unintended adverse effect on any part of the game. The Critical Severity should only be used for issues that will actually crash the game or make it otherwise unplayable.

Finally, Suggestions should only be used sparingly. All diagnostics, regardless of Severity, should have documentation and reasoning behind them. With the Critical and Warning Severities those reasons are often fairly simple and short: things don't work. The issue with Suggestions is that they don't talk about problems but about ideas.

If you want to create Suggestions, you should investigate thoroughly if whatever you're suggesting, is actually going to positively affect the users experience. You **should not** create Suggestions for "common beliefs", anecdotes, or "works on my machine"-like ideas.

### Summary

The Summary is a short one-sentence description of the diagnostic. In code, you will write a templated string:

```csharp
"Mod {ModA} is not compatible with {ModB}"
```

This string will be rendered on the UI and all fields will be populated with values.

### Details

While the Summary is short and concise, the Details should actually explain the diagnostic and how the user might be able to fix it:

```markdown
Mod {ModA} overwrites {FileA} from Mod {ModB}. This will cause a runtime exception when loading the game.

You can fix this issue by not allowing {ModA} to overwrite {FileA}.
```

You can use markdown formatting in the Details.

## Diagnostic Emitters

An emitter is the component that actually creates the diagnostics. There is currently only one type of emitter:

- `ILoadoutDiagnosticEmitter`

### Loadout Diagnostic Emitter

You should use this emitter if you need to look at the entire Loadout. This emitter will be called whenever the Loadout changes.

## Examples

See the [Examples projects](https://github.com/Nexus-Mods/NexusMods.App/tree/main/src/Examples) on how to use Diagnostics in code.
