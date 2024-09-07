🧰 Unfucked
===

[![GitHub Actions](https://img.shields.io/github/actions/workflow/status/Aldaviva/Unfucked/dotnet.yml?branch=master&logo=github&label=Build)](https://github.com/Aldaviva/Unfucked/actions/workflows/dotnetpackage.yml) [![Testspace](https://img.shields.io/testspace/tests/Aldaviva/Aldaviva:Unfucked/master?passed_label=passing&failed_label=failing&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA4NTkgODYxIj48cGF0aCBkPSJtNTk4IDUxMy05NCA5NCAyOCAyNyA5NC05NC0yOC0yN3pNMzA2IDIyNmwtOTQgOTQgMjggMjggOTQtOTQtMjgtMjh6bS00NiAyODctMjcgMjcgOTQgOTQgMjctMjctOTQtOTR6bTI5My0yODctMjcgMjggOTQgOTQgMjctMjgtOTQtOTR6TTQzMiA4NjFjNDEuMzMgMCA3Ni44My0xNC42NyAxMDYuNS00NFM1ODMgNzUyIDU4MyA3MTBjMC00MS4zMy0xNC44My03Ni44My00NC41LTEwNi41UzQ3My4zMyA1NTkgNDMyIDU1OWMtNDIgMC03Ny42NyAxNC44My0xMDcgNDQuNXMtNDQgNjUuMTctNDQgMTA2LjVjMCA0MiAxNC42NyA3Ny42NyA0NCAxMDdzNjUgNDQgMTA3IDQ0em0wLTU1OWM0MS4zMyAwIDc2LjgzLTE0LjgzIDEwNi41LTQ0LjVTNTgzIDE5Mi4zMyA1ODMgMTUxYzAtNDItMTQuODMtNzcuNjctNDQuNS0xMDdTNDczLjMzIDAgNDMyIDBjLTQyIDAtNzcuNjcgMTQuNjctMTA3IDQ0cy00NCA2NS00NCAxMDdjMCA0MS4zMyAxNC42NyA3Ni44MyA0NCAxMDYuNVMzOTAgMzAyIDQzMiAzMDJ6bTI3NiAyODJjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjY3IDE0LjY3LTEwNiA0NHMtNDQgNjUtNDQgMTA3YzAgNDEuMzMgMTQuNjcgNzYuODMgNDQgMTA2LjVTNjY2LjY3IDU4NCA3MDggNTg0em0tNTU3IDBjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjgzIDE0LjY3LTEwNi41IDQ0UzAgMzkxIDAgNDMzYzAgNDEuMzMgMTQuODMgNzYuODMgNDQuNSAxMDYuNVMxMDkuNjcgNTg0IDE1MSA1ODR6IiBmaWxsPSIjZmZmIi8%2BPC9zdmc%2B&label=Tests)](https://aldaviva.testspace.com/spaces/285777) [![Coveralls](https://img.shields.io/coveralls/github/Aldaviva/Unfucked?logo=coveralls&label=Test%20Coverage)](https://coveralls.io/github/Aldaviva/Unfucked?branch=master)

*Fix egregiously broken or missing functionality in .NET libraries. Inspired by underscore, jQuery, Apache Commons, Spring, and Guava.*

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3" -->

- [Packages](#packages)
    - [General](#general)
    - [Caching](#caching)
    - [Compression](#compression)
    - [DateTime](#datetime)
    - [DI](#di)
    - [DNS](#dns)
    - [ICS](#ics)
    - [OBS](#obs)
    - [PGP](#pgp)
    - [STUN](#stun)
    - [Twitch](#twitch)
    - [Windows](#windows)
- [Related packages](#related-packages)

<!-- /MarkdownTOC -->

[![Unfuck your house](https://raw.githubusercontent.com/Aldaviva/Unfucked/master/.github/images/frame.jpg)](https://loadingreadyrun.com/videos/view/2484/Crapshots-Ep270-The-Home-Show)

## Packages

### General
[![NuGet](https://img.shields.io/nuget/v/Unfucked?logo=nuget&label=Unfucked%20on%20NuGet)](https://www.nuget.org/packages/Unfucked)
- Console
    - Colored text and background in strings, not tightly coupled to `Console.Write`
    - Enable colored output on Windows 10 1511 and later
    - Clear screen and move to top-left corner
- Cryptography
    - Random string generation
    - Is certificate temporally valid
- Decimal math
    - `Math` operations on 128-bit `decimal` values
- Directories
    - Delete without throwing an exception on missing directories
- DNS
    - Fluent resolving method
- Enumerables
    - Filter out `null` values
    - Add multiple values at once
    - Upsert into non-concurrent dictionary
    - Fluent set difference method
    - Filter out consecutive duplicate values
    - Convert `IAsyncEnumerator` to list
    - First item or `null` instead of `default` for value types
    - Head and tail
    - Delta changeset between two enumerables (created, updated, deleted, and unchanged items)
    - Atomic swap on `ConcurrentDictionary` to upsert new value and get old value
- Paths
    - Trim trailing slashes
    - Create new empty temporary subdirectory in specific parent directory
    - Convert DOS backslashes to Unix forward slashes
    - Match file extension against a set
- Processes
    - Command line argument marshalling with correct escaping and quoting
        - String to array
        - Array to string
    - Run program and get output and exit code
    - Get parent process of a process
    - Get descendant processes recursively of a process
- Strings
    - Coerce empty strings to `null`
    - Fluently check if a string has any non-whitespace characters
    - Fluently check if a string has any characters
    - Fluent join method
    - Uppercase first letter
    - Lowercase first letter
    - Trim multiple strings from start, end, or both
    - Join enumerable into an English-style list with commas an a conjunction like `and`
    - Fluent conversion method to byte array
    - Fluent conversion method to byte stream
    - Convert DOS CRLF line breaks to Unix LF line breaks
    - Repeat a string a certain number of times
- Tasks
    - Unbounded delay time (.NET 6 tops out at 49.7 days)
    - Await multiple tasks and proceed when any of them both completes and the return value passes a predicate, or they all fail to complete or the predicate
        - Return `true` if any passed or `false` if they all failed
        - Return the first passing task's result, or `null` if they all failed
    - Asynchronously await the cancellation of a `CancellationToken` without blocking the thread
- URIs
    - Fluent method to get URI query parameters
    - Check if a URI host belongs to a given domain (site locking)
- XML
    - Fluent method to read an XML document from an HTTP response body
    - Find all descendant elements of a parent node which have a given tag name

### Caching
[![NuGet](https://img.shields.io/nuget/v/Unfucked.Caching?logo=nuget&label=Unfucked.Caching%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.Caching)
- For use with `System.Runtime.Caching`
- Type-safe `MemoryCache<T>`

### Compression
[![NuGet](https://img.shields.io/nuget/v/Unfucked.Compression?logo=nuget&label=Unfucked.Compression%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.Compression)
- For use with SharpCompress
- TAR archives
    - Symlinks
    - Directories
    - File owner
    - File group
    - File permissions

### DateTime
[![NuGet](https://img.shields.io/nuget/v/Unfucked.DateTime?logo=nuget&label=Unfucked.DateTime%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.DateTime)
- For use with NodaTime
- Absolute value of a `Duration` or `TimeSpan`
- Get midnight from a `ZonedDateTime` as a `ZonedDateTime`, `Period`, or `Duration`
- Convert time zone offset to hours
- Compare two `OffsetDateTimes` to see which one happens first

### DI
[![NuGet](https://img.shields.io/nuget/v/Unfucked.DI?logo=nuget&label=Unfucked.DI%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.DI)
- For use with `Microsoft.Extensions.Hosting` dependency injection/inversion of control
- Add a colored console with column formatted data
- Search for JSON configuration files in executable directory, not just current working directory
- Allow provider functions to be injected, so long-lived consumers can depend on short-lived dependencies and control their lifecycle

### DNS
[![NuGet](https://img.shields.io/nuget/v/Unfucked.DNS?logo=nuget&label=Unfucked.DNS%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.DNS)
- For use with G6.GandiLiveDns
- Interface for Gandi LiveDNS HTTP API client to allow mocked testing isolation

### ICS
[![NuGet](https://img.shields.io/nuget/v/Unfucked.ICS?logo=nuget&label=Unfucked.ICS%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.ICS)
- For use with Ical.Net
- Asynchronously serialize iCalendar files to a byte stream, to prevent Kestrel and IIS errors on synchronous I/O
- Converters between the three implementations of a datetime used by Ical.Net, NodaTime, and .NET

### OBS
[![NuGet](https://img.shields.io/nuget/v/Unfucked.OBS?logo=nuget&label=Unfucked.OBS%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.OBS)
- For use with OBSClient
- Interface for Open Broadcaster Software Studio WebSocket API client to allow mocked testing isolation

### PGP
[![NuGet](https://img.shields.io/nuget/v/Unfucked.PGP?logo=nuget&label=Unfucked.PGP%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.PGP)
- For use with PgpCore
- Detached signing, where an ASCII-armored PGP signature is generated in a sidecar file, not inline wrapping the file being signed like in clearsigning

### STUN
[![NuGet](https://img.shields.io/nuget/v/Unfucked.STUN?logo=nuget&label=Unfucked.STUN%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.STUN)
- For use with Stun.Net
- STUN client that retains the server that it was configured with, useful for error logging
- Multi-server STUN client that transparently picks a random public STUN server from a constantly-updated list when sending a request, with retries and fallbacks if any of them fail
- Thread-safe multi-server STUN client that can be used concurrently from multiple threads without conflicts

### Twitch
[![NuGet](https://img.shields.io/nuget/v/Unfucked.Twitch?logo=nuget&label=Unfucked.Twitch%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.Twitch)
- For use with TwitchApi.Net
- Interface for Twitch HTTP API client to allow mocked testing isolation

### Windows
[![NuGet](https://img.shields.io/nuget/v/Unfucked.Windows?logo=nuget&label=Unfucked.Windows%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.Windows)
- Reliably detect when computer is entering and exiting standby
- Kill the running screensaver

## Related packages

#### Bom.Squad
[![NuGet](https://img.shields.io/nuget/v/Bom.Squad?logo=nuget&color=blue&label=Bom.Squad%20on%20NuGet)](https://www.nuget.org/packages/Bom.Squad)

Reconfigure the static shared `Encoding.UTF8` instance to not emit UTF-8 byte order marks to unbreak compatibility with non-Microsoft software.

#### DarkNet
[![NuGet](https://img.shields.io/nuget/v/DarkNet?logo=nuget&label=DarkNet%20on%20NuGet)](https://www.nuget.org/packages/DarkNet)

Turn native Windows title bars dark in WPF and Windows Forms windows.

#### DataSizeUnits
[![NuGet](https://img.shields.io/nuget/v/DataSizeUnits?logo=nuget&label=DataSizeUnits%20on%20NuGet)](https://www.nuget.org/packages/DataSizeUnits)

Parse, convert, normalize, and format amounts of data like bits, bytes, kilobytes, and megabytes.

#### HidClient
[![NuGet](https://img.shields.io/nuget/v/HidClient?logo=nuget&label=HidClient%20on%20NuGet)](https://www.nuget.org/packages/HidClient)

Automatically connect and reconnect to USB HID peripherals.

#### KoKo
[![NuGet](https://img.shields.io/nuget/v/KoKo?logo=nuget&label=KoKo%20on%20NuGet)](https://www.nuget.org/packages/KoKo)

Automatically implement INotifyPropertyChanged to allow you to create composable properties with no publisher or subscriber boilerplate.

#### ThrottleDebounce
[![NuGet](https://img.shields.io/nuget/v/ThrottleDebounce?logo=nuget&label=ThrottleDebounce%20on%20NuGet)](https://www.nuget.org/packages/ThrottleDebounce)

Throttle, debounce, and try functions and actions.

#### UnionTypes
[![NuGet](https://img.shields.io/nuget/v/UnionTypes?logo=nuget&label=UnionTypes%20on%20NuGet)](https://www.nuget.org/packages/UnionTypes)

Implicitly cast multiple types to one union type, to avoid manually writing millions of method overloads.