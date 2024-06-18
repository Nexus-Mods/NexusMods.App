```
status: accepted
date: {2023-10-10}
deciders: {App Team @ 2023-10-10 in team discussions}
```

# Multiple application instances support and implementation design

## Context and Problem Statement

Multiple application instances support means allowing users to open multiple windows of the App and perform operations
from them without incurring in errors, race conditions or general weirdness.

This is desirable for the following scenarios:

- Users modding different games at the same time.
- Users wanting to compare different loadouts/profiles/modlists.
- Users wanting better use screen real-estate in multiple monitor setups.

This is technically problematic for the following reasons:

- Requires communication and coordination across different Application processes.
- Requires advanced synchronization of the DataModel, potentially more than what offered by peer access to Sqlite DB.
- Requires synchronization for Apply step as well as for the management of the current state of the game folder.
- Requires consideration in the Application design, removing assumptions on what the "current" state actually is, since
  another instance might be contradicting it.
- E.g. Two Loadoutds for the same game are opened in two different instances and a mod download finishes. In which of
  the two loadouts does the mod get installed? Same for "Current Game" situations.

### Current situation:

Currently the App does allow opening multiple instances at the same time. This is done through the use of two Sqlite db
instances, one for the DataModel information, another used as a makeshift Inter-Process Communication tool.

All application instances behave as peers when accessing the DB instances, and this can cause inefficiencies and delays.
In particular the IPC part of the solution has caused many issues with long delays and dropped messages, causing many of
the flaky tests failures.

The application UI is usually able to correctly reflect latest state of the DataModel, thanks to reactive Data binding,
but the application is still fairly simple and properly supporting multi instance might become harder as more features
and more complex functionalities are added.
Already some problems exist with Apply and loadout selection for downloads.

## Considered Options

1. Forgo Multi-Instance support completely
2. Offer only limited multi instance support, for power-users, with disclaimers and known limitations.
3. Support Multi-Instance using the current Sqlite based peer access architecture
4. Support Multi-Instance using current peer architecture, but move away from Sqlite for the IPC part and use ad-hoc IPC
   solution (not many that are multiplatform).
5. Support Multi-Instance with new IPC solution and a centralized master process in charge of synchronization and
   DataModel db access.
6. Single instance, but with CLI IPC

```md
## Decision Outcome

Chosen option: "{title of option 1}", because
{justification. e.g., only option, which meets k.o. criterion decision driver | which resolves force {force} | … | comes out best (see below)}.

<!-- This is an optional element. Feel free to remove. -->
### Consequences

* Good, because {positive consequence, e.g., improvement of one or more desired qualities, …}
* Bad, because {negative consequence, e.g., compromising one or more desired qualities, …}
* … <!-- numbers of consequences can vary -->

<!-- This is an optional element. Feel free to remove. -->
## Validation

{describe how the implementation of/compliance with the ADR is validated. E.g., by a review or an ArchUnit test}
```

## Pros and Cons of the Options

### 1. Forgo Multi-Instance support completely

Restrict application to only one instance at a time. This behaviour is pretty common for many applications and not
unreasonable.
Though, experiences from other Managers (Vortex, MO2) have shown that some users would still like to be able to open
multiple instances, even at the cost of potential inconsistency.

#### Good, because:

* Application complexity is greatly reduced
* No IPC required
* No design considerations regarding state across multiple processes
* No synchronization considerations across processes

#### Bad, because:

* We already know some users will want the feature.
* No support for using multiple windows on many monitors
* This can have consequences on testing and development
* Accepting this simplified design will make it much harder to ever add this feature in the future, even in limited
  capacity.

### 2. Offer only limited multi instance support, for power-users, with disclaimers and known limitations.

This is what MO2 is currently doing, offering multi instance support hidden behind a command line parameter.
The main use case is to that of using the second window as reference to compare with another managed instance of the
same game.

#### Good, because:

* We can blame users when stuff goes wrong?
* Only core features need to be designed with multi instance in mind
* Complexity only needs to go as far as developers are comfortable with.

#### Bad, because:

* Few users would actually ever get to discover/use the feature
* We would slowly blame more and more on users, and our code would appear buggy.
* Still need to setup multi instance support infrastructure for core features
* Unsupported "official" features tend to give a bad impression of the design of the app. Do it right or don't do it at
  all.

### 3. Support Multi-Instance using the current Sqlite based peer access architecture

#### Good, because:

* No process is the true owner, no reason to manage the "main" process, peers can join/leave as needed
* Has opt-in performance costs. If only one process is accessing the data, no complex broadcasting of events needs to be
  performed

#### Bad, because:

* Sqlite has some strange behavior with multi process access that sometimes results in locked tables
* Locks can mean that data is written but reading the data just written can lock/pause.
* I (Tim) have observed some behavior that makes me think that readers can sometimes lag behind writers quite a bit.
  That secondary processes may have to wait a second or two for writes to become visible. Essentially caches may not
  always cashe as we would assume.

### 4. Support Multi-Instance using current peer architecture, but switch IPC solution

#### Good, because:

* Doesn't require much of a change in the current code base

#### Bad, because:

* Doesn't solve the dirty reads issue, where the IPC may send information about a new entity, but that information
  hasn't been populated to the other instances yet
* Cross-platform IPC without a centralized main process is a bit hard to implement, each process would need a named pipe
  or local TCP port.
* Shared memory IPC can be difficult because the size of the shared memory must be decided up-front (several OSes don't
  allow memory mapped windows to resize). Any Shared memory system also requires some sort of communication/lock system
  to signal that a new message is available
* Still has an issue of updates from IPC going through a different code path from updates from the local process.

### 5. Support Multi-Instance, switch IPC solution, use centralized master process for DataModel db access.

#### Good, because:

* Forces us to segment the backend and front end code.
* We can no longer share objects between the front and backend
* Performing atomic updates of modlists is a lot simpler as we can lock inside the main process and perform that lock
  in-memory instead of via a multi-process mutex
* We can cache any immutable data once for all processes
* We drastically reduce the reads sent to the datastore. If something writes to a key, we can cache that data and don't
  need to read it if we already know what's written to the store.
* We can use any backend datastore we want. FasterKV (from Microsoft) is optimized for high numbers of writes, and
  limited reads.
* We can standardize on one serialization format, our IPC format can be the same as the disk format
* Possible faster startup time for CLI processes. Perhaps even with a drastically smaller CLI process
* If the communication method is something that can run over TCP, we could remotely manage one system from another
  system, which may be useful for Steam Deck or VR headset support
* Provides a nice testing point. We can mock the backend and test against the IPC communication layer

#### Bad, because:

* Debugging multiple processes at once can be a major headache
* There aren't any off-the shelf solutions that do what we want here. We use polymorphism rather heavily and there
  aren't any good solutions for this with gRPC, REST, or GraphQL.
* A lot of the common remoting systems (aside from gRPC) are designed internet level communications and have rather high
  overheads when dealing with small payloads (<1KB). So we'd likely need to build on top of websockets or something like
  that
* Management of the main process won't be super complicated but will be error prone. We'll need to restart the main if
  it crashes, deal with startup storms (10 clients all trying to restart the main process at once after it dies). and
  we'll need a way to guarentee "exactly one" semantics on startup.

### 6. Single instance, but with CLI IPC

Launching the CLI without the UI running would launch the UI. Once the UI is launched, future invocations of the UI
would load a new window in the same UI process. We could have the CLI pass flags into the UI when it starts it to have
the UI start minimized, in the system tray, etc. The UI could auto-exit if it was created from the CLI and it's been
idle for awhile.

#### Good, because

* The app becomes single instance, allowing all work to take place in a single process
* We only have to do IPC for CLI interactions, which are all text based and type free
* CLI could become a *very* small AOT compiled program that connects to a socket (or pipe). Could be almost completely
  application agnostic. All the CLI processing code, parsing, etc. can live in the UI
* 3rd party apps could connect via this same text based interface, or use the CLI
* We remove all need for serialization of data across IPC
* It might be possible to run our CLI libraries (https://spectreconsole.net/) over a socket? So the CLI just becomes a
  terminal redirect to a socket? (this may be crazy talk)

#### Bad, because

* UI must always be active to use the CLI
* If a CLI task is running, we'd need to restrict closing the UI

## Chosen Option

Option 6, because: it provides a balance between the complexity of implementation and the desired features of the CLI.
It also allows
us to keep the UI as the main entry point for the application, while still allowing the CLI to be used as a tool for
automation and scripting.
The CLI already has an abstraction of strings passed in on the commandline, and results passed back via stdin/stdout, so
it's not a big leap to
put this over a socket instead.
