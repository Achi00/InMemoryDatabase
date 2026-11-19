# InMemoryDatabase
 
A Redis-compatible in-memory key-value database written from scratch in C#/.NET.

It speaks the Redis wire protocol (RESP2) over TCP, supports strings, integers, expiration and lists, and is built around low-allocation parsing with `System.IO.Pipelines`. Unlike Redis, it executes commands **in parallel** across all CPU cores, which makes thread safety a central part of the design.
 
This is a learning and portfolio project. The goal is not to replace Redis, but to understand and implement the hard parts of a network database: protocol parsing, concurrency, expiration, and connection lifecycle.

## Features
- **RESP2 protocol**: simple strings, errors, integers, bulk strings, arrays, and null values, plus **inline commands** (`SET foo bar` typed directly into `nc` or `telnet`)

commands can be sent both ways

```bash
*3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$3\r\nbar\r\n
SET foo bar\r\n
```

- **Pipelining**: multiple commands in one network packet are parsed and answered in order
- **Strings and integers**: `SET` with `EX` / `PX` / `NX` / `XX`, `GET`, atomic `INCR` / `DECR` / `INCRBY` / `DECRBY` with overflow protection built in custom parser `RespParser`
- **Expiration**: lazy expiration on access plus a Redis-style active sampler that reclaims memory from keys nobody reads
- **Lists**: `LPUSH`, `RPUSH`, `LPOP`, `RPOP`, `LLEN`, `LRANGE`, with Redis semantics (automatic creation, automatic deletion when empty, negative indexes)
- **Type safety across keys**: one keyspace for all types, `WRONGTYPE` errors when a command targets the wrong kind of value
- **Graceful shutdown**: in-flight commands finish and reply, idle connections close, stuck connections are aborted after a deadline
- **Hardened parser**: limits on nesting depth, array size, bulk string size, line length, and stalled commands

## Supported commands
 
| Group | Commands |
|---|---|
| Connection | `PING` |
| Strings | `SET key value [EX seconds \| PX milliseconds] [NX \| XX]`, `GET` |
| Integers | `INCR`, `DECR`, `INCRBY`, `DECRBY` |
| Keys | `DEL key [key ...]`, `EXISTS key [key ...]`, `EXPIRE`, `TTL` |
| Lists | `LPUSH`, `RPUSH`, `LPOP`, `RPOP`, `LLEN`, `LRANGE` |

Command names are case-insensitive. Replies and error messages follow Redis conventions (`-ERR ...`, `-WRONGTYPE ...`, `$-1` for null).

## Getting started
### Run the server
 
```bash
dotnet run --project InMemoryDatabase
```
 
The server listens on `localhost:6380` (one port above Redis's default, so both can run side by side, no purpose of it I just randomly choose it...).
 
### Try it
 
Inline commands, the easiest way to experiment:
 
```bash
printf 'SET greeting hello\r\nGET greeting\r\n' | nc -q1 localhost 6380
# +OK
# $5
# hello
```
 
The full RESP array form, as client libraries send it:
 
```bash
printf '*3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$3\r\nbar\r\n' | nc -q1 localhost 6380
# +OK
```
 
Lists and expiration:

`Lists are not fully finished might have some issues`
 
```bash
printf 'RPUSH tasks email backup report\r\nLRANGE tasks 0 -1\r\nLPOP tasks\r\n' | nc -q1 localhost 6380
printf 'SET session abc EX 10\r\nTTL session\r\n' | nc -q1 localhost 6380
```
 
### Run the tests
 
```bash
dotnet test
```
 
## Architecture
 
```
TcpListener -- accept --> one task per connection
                            |
                     PipeReader (System.IO.Pipelines)
                            |  raw bytes
                     RespParser ------------ array form or inline form
                            |  RespValue
                     RespCommandExecutor --- name --> ICommandHandler (Strategy pattern for better scaling for future commands, no modification needed, just register in DI)
                            |
                     Command handler ------- validates arguments, calls the store
                            |
                     RespStore ------------- one keyspace: strings, lists, expiry, partial class, RespStore.Lists and RespStore.Strings
                            |  result
                     RespWriter --> PipeWriter --> socket
```

The layers have strict responsibilities:
 
- **`RespParser`** is a pure, stateless static class: bytes in, `RespValue` out. It has no knowledge of sockets, which makes it testable with plain byte arrays.
- **`RespConnectionHandler`** owns the per-connection loop: reading, parsing, executing, flushing, timeouts and shutdown signals.
- **Command handlers** translate between the protocol and the store. They parse arguments and build replies; they never touch storage internals.
- **`RespStore`** owns all data and all concurrency rules. It is split across partial files by topic (`RespStore.cs`, `RespStore.Strings.cs`, `RespStore.Lists.cs`) while remaining one class with one dictionary.
- **`RespValue`** describes what travels over the wire. **`StoredValue`** (`StringValue`, `ListValue`) describes what is stored. Keeping protocol types and storage types separate lets each evolve independently.
Hosting uses the .NET Generic Host (`Microsoft.Extensions.Hosting`): the server and the expiry sampler run as hosted services, handlers are registered through dependency injection, and Ctrl+C / SIGTERM trigger a graceful stop.
 

### Parallel execution instead of a single thread
 
Redis executes all commands on a single thread. That removes the need for locks entirely: every command is atomic by construction. The cost is that command execution is limited to one CPU core, and Redis scales by running multiple instances.
 
This project deliberately takes the other path: every connection runs on the .NET thread pool, and commands from different clients execute **concurrently** against shared state. The motivation is to practice the concurrency patterns that real multi-threaded .NET services need, and to make the tradeoffs concrete:

`I know redis single threaded design will be more performant but I just wanted to learn more about locking and thread safety`

### Low-allocation protocol parsing
 
The network path is built on `System.IO.Pipelines`, `ReadOnlySequence<byte>` and `SequenceReader<byte>`:
 
- Incoming bytes are parsed **in place** from pooled pipe buffers. Slicing a bulk string or finding a line terminator creates views over existing memory, not copies.
- **Multi-segment data** is handled explicitly. When a value happens to span two buffer segments, the fast path (`IsSingleSegment` → `FirstSpan`) is skipped and the bytes are stitched into a small contiguous buffer: `stackalloc` for integer lines, which are bounded to 20 bytes, and `ArrayPool<byte>` for longer inline lines.
- **Constant byte sequences** such as `\r\n` are exposed as `static ReadOnlySpan<byte>` properties. The compiler embeds them in the assembly's data section, so they cost no allocation and no stack space at runtime.
- **Inline commands** are tokenized directly from bytes, without building an intermediate string for the whole line.
- `PipeReader.AdvanceTo(consumed, examined)` is used correctly, so a partially received command does not cause the loop to spin on the same bytes.
Remaining allocations are deliberate and documented: each parsed value becomes a `string`, and each array becomes a `RespValue[]`, because parsed commands must outlive the receive buffer. A byte-based storage and dispatch pass is on the roadmap.

### Defensive parsing
 
Every length on the wire is controlled by the client, so every one is bounded:
 `All explicitly set in RespParser, I will need to test it further and harder to see limits and how much safety net this is`
| Guard | Limit | Protects against |
|---|---|---|
| Array nesting depth | 32 | stack overflow through deeply nested arrays |
| Array element count | 1,000,000 | out-of-memory from a huge declared count |
| Bulk string length | 512 MB | out-of-memory from a huge declared length |
| Inline / simple line length | 64 KB | unbounded buffering of a line without a terminator |
| Integer line length | 20 bytes | unbounded `stackalloc` |
| Incomplete command | 10 s without new data | clients that stall mid-command |
 
The parser distinguishes two failure kinds with separate exception types:
 
- **Incomplete data** (`RespIncompleteDataException`): the command hasn't fully arrived. The connection waits for more bytes.
- **Protocol violation** (`RespProtocolException`): the bytes can't be valid RESP. The connection is closed, since the server can no longer tell where the next command starts.
Command-level mistakes, such as a wrong argument count or a non-integer value, are not exceptions at all: they produce a normal `-ERR` reply, and the connection stays usable.
It comes from `RespValue` struct.

### Expiration: lazy plus active sampling
Expiration follows Redis's two-part design:
 
- **Lazy expiration** guarantees correctness. Every read checks the entry's expiry, and an expired key behaves as if it doesn't exist. Eviction is race-safe: an expired entry is removed only if it is still the exact entry that was read (`TryRemove(KeyValuePair)`), so a value written by another client at the same moment is never deleted.
- **Active sampling** reclaims memory. Lazy expiration alone would keep expired-but-never-read keys in memory forever. A background service samples 20 random keys with a TTL every 100 ms, deletes the expired ones, and repeats immediately while more than 25% of a sample is expired, within a 10 ms time budget per cycle. Work adapts to need and is always capped.
Keys with a TTL are tracked in a dedicated `VolatileKeySet`, so the sampler never wastes work on permanent keys. It combines a `List<string>` for O(1) random access with a `Dictionary<string, int>` index, and uses **swap-remove** for O(1) deletion. Its internal invariant (every key is at the position its index records) has a dedicated check used by the tests.

### Graceful shutdown
 
Clients rely on one rule: **if I received a reply, the command ran; if I didn't, it didn't.** Cancelling a connection between executing a command and flushing its reply would break that rule, for example an `INCR` that was applied but never acknowledged.
 
Shutdown therefore uses two separate signals:
 
1. **Stop accepting** new connections.
2. **Stop reading** (soft signal): idle connections close immediately; connections with commands already received execute them and flush the replies, then close.
3. **Wait** for all tracked connection tasks, up to a grace period.
4. **Abort** (hard signal) only if the grace period expires, interrupting stuck connections such as clients that stopped reading.
Connections are tracked in a `ConcurrentDictionary` of tasks, and each one removes itself on completion, so the collection always holds exactly the live connections.
