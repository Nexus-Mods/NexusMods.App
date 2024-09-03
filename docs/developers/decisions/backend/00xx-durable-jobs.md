# Durable Job System

## Context and Problem Statement

Over the course of development of the Nexus Mods app, the need for a job system has arisen. This job system needs to handle 
the various background tasks and long running "jobs" that the app will need to perform. These jobs include but are not limited to:

* Downloading files
* Hashing files
* Extracting archives
* Gathering metadata about downloaded files

Often these jobs need to be paused, resumed, or restarted. They also need to be able to be run in parallel, and in some cases
should automatically restart if the app terminates unexpectedly.

## Decision Drivers

We have identified the following decision drivers for this decision:

* Durability - Jobs should be able to be resumed after an unexpected termination of the app.
* Parallelism - Jobs should be able to be run in parallel
* Start/Stop/Cancelled - Jobs should be able to be paused, resumed, and cancelled.
* Composite Jobs - Jobs should be able to be composed of other jobs.
* Type Safety - Jobs should be type safe and not require casting when data is passed between jobs.
* Relatively normal C# code - The job system should not require a lot of boilerplate code to use.
* Simple implementation - The job system should be simple to implement and understand and not require a lot of external dependencies.
* Progress Reporting - Jobs should be able to report progress to the UI both as deterministic progress and as indeterminate progress.

Not Decision Drivers:

* Performance - It is assumed that the most performance critical code will be in pure C# code and that the dispatching of these
routines will be relatively simple. Thus concepts like allocation free code are not a priority for this decision.
* Scalability - It is assumed that the app will be running on a single machine and that the job system will not need to scale to multiple nodes.

## Considered Options

* [Hangfire](https://www.hangfire.io/) - a durable job system for .NET
  * Good, because it is a mature and well tested system
  * Good, as it handles several of the concerns we have
  * Bad, because it wants jobs to be written as Fluent method call chains instead of pure C# code
  * Bad, because it is a large dependency with a lot of external dependencies like a storage server
  * Bad, because it is designed around multiple machine fault tolerance
* [Durable Task Framework](https://github.com/Azure/durabletask) - a durable task framework for .NET
  * Good, because it is a mature and well tested system
  * Good, it allows for writing complex workflows as pure C# code
  * Bad, because it is a large dependency with a lot of external dependencies like a storage server
  * Bad, because it is designed around multiple machine fault tolerance
  * Bad, because no single backend is one we would want to use
  * Bad, because it prefers to be run in Azure and an isolated context
* [Akka.NET](https://getakka.net/) - a distributed actor system for .NET
  * Good, because it is a mature and well tested system
  * Bad, because it wants code to be written as actors instead of "normal" C# code
  * Bad, because it is a large dependency with a lot of external dependencies like a storage server
  * Bad, because it is designed around multiple machine fault tolerance
  * Bad, because it is designed around a distributed system
  * Bad, because it is more of an actor system than a job system
* [Orleans](https://dotnet.github.io/orleans/) - a distributed actor system for .NET
  * Good, because it is a mature and well tested system
  * Bad, because it wants code to be written as actors instead of "normal" C# code
  * Bad, because it is a large dependency with a lot of external dependencies like a storage server
  * Bad, because it is designed around multiple machine fault tolerance
  * Bad, because it is designed around a distributed system
  * Bad, because it is more of an actor system than a job system
* Custom Durable Functions - a custom implementation of the "good parts" of the Durable Task Framework
  * Good, because it is simple to implement and understand
  * Good, because it has no external dependencies (aside from ones we already use)
  * Good, because it allows for writing complex workflows as pure C# code
  * Good, because it is designed around a single machine fault tolerance
  * Bad, because it is not as mature as the other options

## Decision Outcome

A custom option was chosen as it allows us the maximum flexibility in implementation and the least amount of external dependencies.

As it turns out, the basics of a Durable Task Framework are fairly easy to implement. The basic premise is as follows: 

1. A job is a class that implements one of the `AJob<T>` base classes
2. Jobs are singletons and are part of the DI system.
3. A job's `Run` method may be called many times, and jobs have a "at least once" guarantee. Meaning they will run once, but may run more than once.
4. All calls to sub-jobs are created via calling a static member on the sub-job class, and passing to it the `context` handed to the parent's `Run` method.
5. Awaiting the result of a sub-job will `snapshot` the current job and replay all the events of the parent job once the child job is complete. This is fairly 
hard to understand without an example, but let's example a simple job:

```csharp

    [Fact]
    public async Task CanRunSumOfSquaresJob()
    {
        var values = new[] { 1, 4, 3, 7, 42 };

        // Create the base job
        var result = await _jobManager.RunNew<SumJob>(values);

        result.Should().Be(values.Select(x => x * x).Sum());
    }
  
    // A job that takes an array of ints and returns the sum of the squares of those ints
    public class SumJob : AJob<SumJob, int, int[]>
    {
        protected override async Task<int> Run(Context context, int[] ints)
        {
            var acc = 0;
    
            // Normal foreach loop with an await inside
            foreach (var val in ints)
            {
                // Create a sub-job and run it.
                // Awaiting the result will "snapshot" the current job. Once the sub-job completes
                // this method will be called and the method will be `replayed` with the result of the sub-job,
                // and execution will continue. 
                acc += await SquareJob.RunSubJob(context, val);
            }
            
            return acc;
        }
    }
    
    public class SquareJob : AJob<SquareJob, int, int>
    {
        protected override Task<int> Run(Context context, int arg1)
        {
            // Normal code here
            return Task.FromResult(arg1 * arg1);
        }
    }
    
```

As can be seen, we can now write jobs as normal C# code. By default, each time a job is "snapshotted" and goes into an "waiting" state, it is persisted
via the `IJobStorage` interface. When the application restarts any saved jobs are reloaded and restarted. This system closely 
follows the design of the Durable Task Framework, but with all the distributed and fault tolerance parts removed. Since this 
code is pure C# code and not a DSL or source generator, we can easily debug and understand the code. In addition the code
can use all the normal C# features like `async` and `await` and `foreach` loops:


```csharp

    public class WaitMany : AJob<WaitMany, int, int[]>
    {
        protected override async Task<int> Run(Context context, int[] inputs)
        {
            // Add all the tasks to a list
            var tasks = new List<Task<int>>();
            foreach (var input in inputs)
            {
                tasks.Add(SquareJob.RunSubJob(context, input));
            }
    
            // Wait for all the tasks to complete
            await Task.WhenAll(tasks);
    
            return tasks.Select(t => t.Result).Sum();
        }
    }
    
    public class AsyncLinqJob : AJob<AsyncLinqJob, int, int[]>
    {
        protected override async Task<int> Run(Context context, int[] ints)
        {
            var sum = 0;
            
            // Even Async Linq works
            await foreach (var val in ints.SelectAsync(async x => await SquareJob.RunSubJob(context, x)))
            {
                sum += val;
            }
            return sum;
        }
    }

```

### Implementation Details

Internally each job becomes an actor (a Task and a queue of messages). Jobs communicate to eachother via messages, and the 
changes and updates of a single job are single threaded, this is done mostly to contain the complexity of the implementation, and to 
allow the use of standard mutable collections in the job code.

The concept of "Replaying" is not very intuitive, but it is easy to understand: 

* When a job is run, it is handed a `Context` object in this object is
  * `HistoryIndex` - a stack pointer of sorts into the history of the job, it starts at 0 
  * `List<HistoryEntry>` - this is the history of the job
    * `HistoryEntry` is a record of the following values
      * `ChildJobId` - The id (a guid) of the child job that was run
      * `Status` - Either `Running`, `Completed`, or `Failed` depending on the state of the sub-job
      * `Job` the singleton instance of the sub-job that was run (for serialization purposes)
      * `Result` - The result of the sub-job if it was completed, this will be an error message if the job failed
* When a call is made to `RunSubJob` the job system will look at the history index
 * If the index is higher than the length of the history:
   * A sub job will be created and started, it's JobId will be added to the history
   * The current history index will be handed to the job as the "parent history index"
 * If the index is less than the length of the history:
   * This means we're replaying the job and we've already spawned this sub-job
   * So we look at the status of the sub-job
     * If it's `Running` we return a `Task.FromException(new WaitException())` which will cause the current replay
 event to abort and the job will be considered to be paused
     * If it's `Completed` we return the result as a `Task.FromResult(result)`
     * If it's `Failed` we return the exeception as a `Task.FromException(new SubJobExecption(result))`
   * Once a job completes:
     * We look to see if the job has a parent Id
      * If so we send the parent a message to set our result to a given value
      * After the value is set, the parent will awake and replay the job
     * If the job has no parent it almost always has a C# continuation, which we will call when it exists

Based on this description a few concerns are likely to arise:

* Yes, the number of times a job is re-run is at least equal to the number of `await` calls in the job.
* History grows linearly with the number of semantic "await" calls in the job. Put a loop with 1000 iterations with an await in it, and the history will grow by 1000 entries.
* The history is persisted to disk on every update, this will result in sub-par performance 
* Parameters passed to and from jobs must be serializable to JSON, so no passing TCP connections or other non-serializable objects
* Code in a job should be idempotent, as it may be run more than once. This means that generation of data should be put into other non-restarting jobs so that their results can be memoized. 


With all of these constraints in mind, the assumption is made that jobs in general will keep most of their complex performance critical logic
outside of these auto-restarting jobs. It is assumed that most of the jobs in our system will have less than a dozen steps and mostly
pass around integers and other primitives. Even MnemonicDB ReadOnly models boil down to essentially 2 ulongs: the TxId and he EntityId.

