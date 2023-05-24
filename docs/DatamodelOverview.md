# Datamodel Overview

The Datamodel provides a set of services that attempt to create a unified interface for interacting with mods, loadouts, 
archives and other parts of the system. 

## The Datastore (IDataStore)
The Datastore is the central repository of information for the app. At its core it's set of key value pairs. Generally 
each key is known as a "Id" and inherits from a "IId". Each Id has a `EntityCategory` enum attached to it that tells the
datastore how to organize the Id. In general one could think of this Category as a one-byte prefix on the Id. In practice
the Datastore stores Ids with a given category in a separate table in the database.

The datastore itself is backed by SQLite, see the ADRs in this repository for more information about why SQLite was chosen
over other options. 

### Ids
IDs all inherit from `IId` and are generally a struct. `IId` know how to write themselves to `Span<byte>` and are stored
in the DB as binary blobl. This means that prefix scans can be done for IDs with a given prefix. Thus one can construct
a 128bit ID and then search the datastore for any entry that is prefixed by the 64bits given in a query. IDs are of variable
size but it is recommended to try and keep all the keys in a given category consistent and to keep the IDs as small as possible. 

### CAS
The Datastore supports a Compare-And-Swap atomic operation on keys in the database. This is useful for change tracking since
all the entities in the Datastore are immutable records.

### Data Storage
Most data in the Datastore is stored as children of the `Entity` Record. By default entities are given an ID that matches
the xxHash64 of the JSON encoded content of the same entity. This is known as "content addressing" and is a simple way to 
generate IDs for immutable entities. Loadouts, Mods, and Files in those mods are all immutable entities. There are methods
on the `Entity` class that can be used to customize serialization. 

### Serialization
Data is stored after serializing entities to UTF-8 JSON. Although the Datastore also supports raw binary values. Serialization
and deserialization is fully polymorphic thanks to the `$type` tag on ever JSON entity written. Currently the value for this
type is stored as a string derived from the `[JsonName("foo")]` attribute on the entities, but likely in the future will
be migrated to GUIDs both for performance and for clarity in design. The Names themselves are currently freeform and completely
ad-hoc. 

Serialization is done using the `System.Text.Json` library. This library is fast and has a very small memory footprint, 
polymorphism supported is added via a set of runtime generated `Func<>` objects generated via `Linq.Expressions`. This is
done so that dynamically discovered plugins or extensions can participate in the serialization process.

In order to register types for the serializer, one only need implement the `ITypeFinder` interface and add it to the DI
system during startup. 

## Interprocess Communication
The app is fully multiprocess using another SQLite database and a memory mapped file to provide rapid communication between
processes.

### Message Passing
The app contains a multi-producer, multi-consumer message queue that is used to pass messages between processes. Participating
in this system requires only requesting `IMessageConsumer<>` and `IMessageProducer<>` from the DI system. The types supplied
to these generic interfaces should be registered with the JSON serializers but aside from that it "just works". Publishing 
a message will result in it being seen by consumers. This is a many-to-many queue, so multiple consumers can receive the same
message, even with the same process. 

### Interprocess Job
The app also includes a interprocess job system that allows one service to notify other processes (and even the same process)
of work that is currently being done. This is not a work distribution queue, but more a "task manager" advertisement system. 

Stale jobs are routinely cleaned out, when the parent process terminates.


