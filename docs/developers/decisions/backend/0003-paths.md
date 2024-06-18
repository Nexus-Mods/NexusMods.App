# Paths

## Context and Problem Statement

1) The .NET runtime uses `string` to represent a path. This goes
   against [nominal typing](../project/0004-use-nominal-typing.md).

2) Different platforms support different path separators. This makes path creation difficult and prevents using
   hardcoded paths.

3) [Case sensitivity](https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/IO/PathInternal.CaseSensitivity.cs)
   is dependent on the platform and file system.

| OS         | [Separator Character] | [Alternative Separator Character] |
|------------|-----------------------|-----------------------------------|
| Windows    | `\\`                  | `/`                               |
| Unix-based | `/`                   | `/`                               |

[Separator Character]: https://learn.microsoft.com/en-us/dotnet/api/system.io.path.directoryseparatorchar#remarks

[Alternative Separator Character]: https://learn.microsoft.com/en-us/dotnet/api/system.io.path.altdirectoryseparatorchar#remarks

| OS      | File System    | Case-sensitive                                     |
|---------|----------------|----------------------------------------------------|
| Windows | [NTFS]         | No when using the Win32 API, Yes when using POSIX. |
| Linux   | [NTFS3]        | No by default, can be changed using mount options. |
| Linux   | ext4 and other | Yes, most POSIX file systems are case-sensitive.   |
| macOS   | [APFS]         | No by default, can be changed with Disk Utility.   |

[NTFS]: https://en.wikipedia.org/wiki/NTFS

[NTFS3]: https://www.kernel.org/doc/html/latest/filesystems/ntfs.html

[APFS]: https://support.apple.com/guide/disk-utility/file-system-formats-dsku19ed921c/mac

## Key Observations

The forward slash `/` can be used across all platforms, while the backwards slash `\\` can only be used on Windows.
Microsoft [recommends](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.directoryseparatorchar#remarks) using
the forward slash `/` character for cross-platforms applications.

Aside from some edge-cases with POSIX compliant file systems, paths can be considered case-insensitive. Having two files
with the _same name_ in the _same folder_ that differ only in capitalization is an anti-pattern and should not be
supported. Archive formats are the biggest culprits of this, and we should recommend that the Nexus Mods Web APIs be updated
to reject archives with those files.

## Decision Outcome

### Directory Separator Character

Paths **must** use the forward slash `/` as the **only** directory separator character.

### Root Directories

Root directories are the only directories that are allowed to end with
a [directory separator character](#directory-separator-character):

- Windows: `C:/`
- Unix-based: `/`

The parent directory of a root directory is **always** the root directory itself, signaling to any consumer that this is
the top of the path hierarchy.

Only classic Windows paths that start with a drive letter are supported. UNC paths or anything else is **not supported
**.

### Path Sanitization

Raw paths that haven't been created programatically but were provided by the OS or by the User, are considered
_unsanitized_ and **must** be sanitized before they can be used.

Sanitization involves the following process:

1. Remove trailing whitespaces.

2. Remove trailing [directory separator characters](#directory-separator-character).

3. On Windows: replace backwards slashes with forwards slashes.

Paths containing relative dots (`..`) are **always** considered unsanitized and **shall not** be resolved in code.

### Value Objects

Value objects **shall** be used for paths.

#### `AbsolutePath`

`AbsolutePath` represents a fully rooted path, meaning it starts with a [root directory](#root-directories) and contains
a **required** `Directory` part and an **optional** `FileName` part. Examples:

| Full Path    | `Directory` | `FileName`       |
|--------------|-------------|------------------|
| `/`          | `/`         | `<empty string>` |
| `/foo`       | `/`         | `foo`            |
| `/foo/bar`   | `/foo`      | `bar`            |
| `C:/`        | `C:/`       | `<empty string>` |
| `C:/foo`     | `C:/`       | `foo`            |
| `C:/foo/bar` | `C:/foo`    | `bar`            |

The `Directory` part **must not** end with a [directory separator character](#directory-separator-character), **unless**
it's a root directory.

#### `RelativePath`

`RelativePath` represents a non-rooted part of a path. Examples:

- `<empty string>`
- `foo`
- `foo/bar`

A `RelativePath` can be nested multiple directories deep (eg: `foo/bar/baz`) and can be hardcoded.
