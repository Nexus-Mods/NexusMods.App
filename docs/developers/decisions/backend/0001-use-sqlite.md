# Use SqlLite as a datastore

!!! todo "Update this, our DataStore is moving to RocksDB after becoming a Single Process Application once again."

## Context and Problem Statement

We need a database of some sort to store key-value pairs in the application.

## Decision Drivers

* Should support arbitrary sized keys and values
* Should support arbitrary database sizes and grow as needed (no 4GB limits)
* Should allow multi-process access so that the CLI can run in parallel with the UI
* Should support prefixed key scans so we can support multi key reverse index lookups
* Should support at least 50,000 writes a second (a single game management like Skyrim involves this many involve this many writes)

## Considered Options

| Option | Arbitrary Sized Keys | Arbitrary Sized Values | Arbitrary Database Size | Multi-Process Access | Prefixed Key Scans | RW/sec                         |
| ------ |----------------------|------------------------|-------------------------|----------------------|--------------------|--------------------------------|
| LMDB   | Yes                  | Yes                    | With restart on resize  | Yes                  | Yes                | 1,200,000 reads / 1920 writes  |
| LiteDB | Yes                  | Yes                    | Yes                     | Yes                  | Yes                | 3230 reads / 517 writes        |
| Sqlite | Yes                  | Yes                    | Yes                     | Yes                  | Yes                | 182,348 reads / 139,489 writes |
| Filesystem | Yes                  | Yes                    | Yes                     | Yes                  | Yes                | 34,072 reads / 7973 writes     |
| Faster KV | Yes                 | Yes                    | Yes                     | No                   | No                 | N/A                            |
| RocksDB | Yes                  | Yes                    | Yes                     | No                   | No                 | N/A                            |

## Benchmark results
For this benchmark we tested the put, get, and "havekey" routines for databases that meet the criteria of being multi-process.
Tests were performed for other transaction modes besides "WAL" for Sqlite, but WAL was by far the fastest of all the options.
"SqlLiteScalar" is a variant of the SqlLite tests that uses tables with multiple columns instead of treating the database purely
as a key value store. Keys in this example were 9 bytes long (common for our usecase) and values were 128 bytes long. There was
no significant performance differences using 9 bytes or 1028 byte values


|  Method |          Type |           Mean |        Error |       StdDev |    Gen0 |    Gen1 |    Gen2 | Allocated |
|-------- |-------------- |---------------:|-------------:|-------------:|--------:|--------:|--------:|----------:|
|     Put |    FileSystem |   125,418.1 ns |  2,340.78 ns |  2,403.80 ns |  0.2441 |       - |       - |    5176 B |
|     Get |    FileSystem |    29,349.1 ns |    581.06 ns |    543.52 ns |  0.3357 |       - |       - |    5752 B |
| HaveKey |    FileSystem |    11,911.0 ns |    230.37 ns |    226.26 ns |  0.0458 |       - |       - |     824 B |
|     Put |        LiteDB | 1,933,521.3 ns | 38,290.43 ns | 83,240.25 ns | 29.2969 | 29.2969 | 29.2969 |  163360 B |
|     Get |        LiteDB |   309,517.3 ns |  6,185.27 ns |  8,466.46 ns | 30.7617 | 30.7617 | 30.7617 |  148657 B |
| HaveKey |        LiteDB |   317,962.0 ns |  6,273.28 ns | 13,503.90 ns | 30.7617 | 30.7617 | 30.7617 |  156145 B |
|     Put |          LMDB |   525,621.3 ns | 10,040.93 ns | 12,331.17 ns |       - |       - |       - |     593 B |
|     Get |          LMDB |       872.0 ns |     17.33 ns |     29.42 ns |  0.0439 |       - |       - |     744 B |
| HaveKey |          LMDB |       835.6 ns |     13.55 ns |     12.68 ns |  0.0353 |       - |       - |     592 B |
|     Put |    Sqlite_WAL |     7,169.4 ns |    120.46 ns |    112.68 ns |  0.1602 |       - |       - |    2696 B |
|     Get |    Sqlite_WAL |     5,484.1 ns |     94.44 ns |     88.34 ns |  0.4578 |       - |       - |    7688 B |
| HaveKey |    Sqlite_WAL |     4,715.3 ns |     92.07 ns |     98.51 ns |  0.1450 |       - |       - |    2448 B |
|     Put | Sqlite_Scalar |    14,251.8 ns |    263.21 ns |    246.21 ns |  0.4425 |       - |       - |    7544 B |
|     Get | Sqlite_Scalar |     8,571.2 ns |    169.00 ns |    187.84 ns |  0.5493 |       - |       - |    9336 B |
| HaveKey | Sqlite_Scalar |     4,714.2 ns |     91.73 ns |     90.09 ns |  0.1450 |       - |       - |    2448 B |



## Decision Outcome

Sqlite with WAL has the best balanced performance, while also allowing for more advanced features like secondary indexes. In addition there are a
lot of tools available for working with Sqlite databases which could help with debugging and interop with other tools.
