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
    - [HTTP](#http)
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
- Comparables
    - Clip a value to a specified range (also known as clamping, limiting, and truncating)
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
    - If you need to resolve anything other than an A or AAAA record, take a look at [DnsClient.NET](https://www.nuget.org/packages/DnsClient) instead
- Enumerables
    - Filter out `null` values
    - Add multiple values at once
    - Upsert into non-concurrent dictionary
    - Fluent set difference method
    - Filter out consecutive duplicate values
    - Convert `IAsyncEnumerator` to list
    - Get the first item, last, singleton, or indexed item, or return `null` instead of `default` for value types
        ```cs
        IEnumerable<TimeSpan> ts = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)];
        TimeSpan t = ts.ElementAtOrNull(100) ?? TimeSpan.FromSeconds(3);
        ```
    - Head and tail
    - Delta changeset between two enumerables (created, updated, deleted, and unchanged items)
    - Atomic swap on `ConcurrentDictionary` to upsert new value and get old value
    - Get or add to `ConcurrentDictionary` and dispose of created but unadded values
    - Get or add to `ConcurrentDictionary` and determine whether a new value was added or an existing value was returned
    - Polyfill for `IList<T>.AsReadOnly` for .NET versions before 8, including .NET Standard
    - Factories for singleton Dictionaries, Sets, and enumerables of key-value pairs
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
    - Polyfill for `StringBuilder.AppendJoin` in .NET Standard 2.0
    - Polyfill for `string.StartsWith(char)` and `string.EndsWith(char)` in .NET Standard 2.0
    - Polyfill for `string.Contains(string, StringComparison)` in .NET Standard 2.0
- Tasks
    - Unbounded delay time (.NET ≥ 6 tops out at 49.7 days, .NET < 6 tops out at 24.9 days)
        ```cs
        await Tasks.Delay(TimeSpan.FromDays(365));
        ```
    - Await multiple tasks and proceed when any of them both completes and the return value passes a predicate, or they all fail to complete or the predicate
        - Return `true` if any passed or `false` if they all failed
            ```cs
            Task<string> a, b;
            bool any = await Tasks.WhenAny([a, b], s => s.Length > 1);
            ```
        - Return the first passing task's result, or `null` if they all failed
            ```cs
            Task<string> a, b;
            string? firstOrDefault = await Tasks.FirstOrDefault([a, b], s => s.Length > 1);
            ```
    - Asynchronously await the cancellation of a `CancellationToken` without blocking the thread, which is especially important to prevent a deadlock if a CancellationToken is used to keep your main thread from exiting
        ```cs
        await cancellationToken.Wait();
        ```
    - Cancel a `CancellationToken` when the user presses <kbd>Ctrl</kbd>+<kbd>C</kbd>
        ```cs
        CancellationTokenSource cts = new();
        cts.CancelOnCtrlC();
        ```
    - Get the result of a task, or `null` if it threw an exception, to allow fluent null-coalescing to a fallback chain, instead of a temporary variable and multi-line `try`/`catch` block statement
        ```cs
        object resultWithFallback = await Task.FromException<object>(new Exception()).ResultOrNullForException() ?? new object();
        ```
- URIs
    - Fluent method to get URI query parameters
    - Check if a URI host belongs to a given domain (site locking)
    - Builder pattern for URLs
    - Truncate URIs to remove the fragment, query parameters, or path. Useful for getting the origin too.
- XML
    - Fluent methods to read an XML document from an HTTP response body as a mapped object, DOM, LINQ, or XPath
    - Find all descendant elements of a parent node which have a given tag name

### Caching
[![NuGet](https://img.shields.io/nuget/v/Unfucked.Caching?logo=nuget&label=Unfucked.Caching%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.Caching)
- Type-safe in-memory cache

### Compression
[![NuGet](https://img.shields.io/nuget/v/Unfucked.Compression?logo=nuget&label=Unfucked.Compression%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.Compression)
- For use with [SharpCompress](https://www.nuget.org/packages/SharpCompress)
- Added features for TAR archives
    - Symlinks
    - Directories
    - File permissions
    - File owner
    - File group

### DateTime
[![NuGet](https://img.shields.io/nuget/v/Unfucked.DateTime?logo=nuget&label=Unfucked.DateTime%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.DateTime)
- For use with [NodaTime](https://www.nuget.org/packages/NodaTime)
- Absolute value of a `Duration` (like `TimeSpan.Duration()`)
    ```cs
    ZonedDateTime a, b;
    var abs = (a - b).Abs();
    ```
- Get midnight from a `ZonedDateTime` as a `ZonedDateTime`, `Period`, or `Duration`
    ```cs
    ZonedDateTime now = SystemClock.Instance.InTzdbSystemDefaultZone().GetCurrentZonedDateTime();
    ZonedDateTime midnight = now.AtStartOfDay();
    Duration sinceMidnight = now.LocalDateTime.TimeOfDay.ToDurationSinceStartOfDay();
    Period sinceMidnight2 = now.LocalDateTime.TimeOfDay.ToPeriodSinceStartOfDay();
    ```
- Convert time zone offset to hours
    ```cs
    double hours = DateTimeZoneProviders.Tzdb["America/Los_Angeles"].GetUtcOffset(SystemClock.Instance.GetCurrentInstant()).ToHours();
    ```
- Compare two `OffsetDateTimes` to see which one happens first
    ```cs
    OffsetDateTime a, b;
    bool isBefore = a.IsBefore(b);
    isBefore = !b.IsAfter(a);
    ```
- More succinct and interchangeable way to define standard `TimeSpan`, `Period`, and `Duration` instances by force casting integral numbers to or constructing `Milliseconds`, `Seconds`, `Minutes`, `Hours`, and `Days`, all of which implicitly convert to `TimeSpan`, `Period`, and `Duration`
    ```cs
    TimeSpan t = (Seconds) 3;
    ```

### DI
[![NuGet](https://img.shields.io/nuget/v/Unfucked.DI?logo=nuget&label=Unfucked.DI%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.DI)
- For use with [Microsoft.Extensions.Hosting](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) dependency injection/inversion of control
- Add a colored console with column formatted data
    ```cs
    HostApplicationBuilder builder = new();
    builder.Logging.AddUnfuckedConsole();
    ```
- Search for JSON configuration files in executable directory, not just current working directory
    ```cs
    HostApplicationBuilder builder = new();
    builder.Configuration.AlsoSearchForJsonFilesInExecutableDirectory();
    ```
- Allow provider functions to be injected, so long-lived consumers can depend on short-lived dependencies and control their lifecycle
    ```cs
    HostApplicationBuilder builder = new();
    builder.Services
        .AddInjectableProviders()
        .AddSingleton<MyService>()
        .AddTransient<MyDependency>();

    class MyService(Provider<MyDependency> dependencyProvider){
        void run(){
            using MyDependency dependency = dependencyProvider.Get();
        }
    }
    ```
- Change the exit code with which the program exits when a background service crashes, instead of 0 which incorrectly indicates success.
    ```cs
    HostApplicationBuilder builder = new();
    builder.Services.SetExitCodeOnBackgroundServiceException(1);
    ```
- Easily register a class in the DI context as both itself and as all of its interfaces automatically, so you can inject it as any of the interfaces without any casting in constructors or unmaintainable multiple registration clutter.
    ```cs
    HostApplicationBuilder builder = new();
    builder.Services.Add<MyService>(registerAllInterfaces: true);

    class MyService: MyInterface;
    class MyDependent(MyInterface dependency);
    ```
- Increase the log message level of specified categories/classes and event IDs, in case the author of the original class foolishly logged important messages with at most the same level as lots of unimportant messages, so you can't just decrease your logger level filter for that entire class.
    ```cs
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    builder.Logging.AmplifyMessageLevels(options =>
        options.Amplify("Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher", LogLevel.Warning, 2, 3, 5, 11, 13, 14, 15, 19, 21, 22, 23, 24));
    ```

### HTTP
[![NuGet](https://img.shields.io/nuget/v/Unfucked.HTTP?logo=nuget&label=Unfucked.HTTP%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.HTTP)
- Inspired by [JAX-RS](https://projects.eclipse.org/projects/ee4j.rest) and [Jersey](https://projects.eclipse.org/projects/ee4j.jersey) for Java
- Drop-in subclasses of `HttpClient` and `HttpMessageHandler` and extension methods
    ```cs
    HttpClient http = new UnfuckedHttpClient(); // set the subclass
    http = new HttpClient(new UnfuckedHttpHandler()); // or set the handler
    http = new UnfuckedHttpClient(new SocketsHttpHandler(){ AllowAutoRedirect = false }); // custom handlers are wrapped automatically
    ```
- Immutable builder pattern for HTTP request URLs, headers, verbs, and response representations that is fluent, composable, and avoids accidentally reusing stale state and sending to corrupted URLs
    ```cs
    JsonNode responseBody = await http.Target("https://httpbin.org")
        .Path("get")
        .Accept(MediaTypeNames.Application.Json)
        .Get<JsonNode>();
    ```
- Request and response filtering
    ```cs
    PrintRequests filter = new PrintRequests();
    http.Register((ClientRequestFilter) filter);
    http.Register((ClientResponseFilter) filter);

    class PrintRequests: ClientRequestFilter, ClientResponseFilter {
        public async Task<HttpRequestMessage> Filter(HttpRequestMessage request, CancellationToken cancellationToken) {
            Console.WriteLine(">> {0} {1}", request.Method, request.RequestUri);
            return request;
        }

        public async Task<HttpResponseMessage> Filter(HttpResponseMessage response, CancellationToken cancellationToken) {
            Console.WriteLine("<< {0} {1}", (int) response.StatusCode, response.RequestMessage?.RequestUri);
            return response;
        }
    }
    ```
- Type-safe property registration
    ```cs
    // set on shared client or ClientConfig
    http.Property(PropertyKey.JsonSerializerOptions, new JsonSerializerOptions()); 

    // set on immutable request target
    WebTarget target = http.Target("https://httpbin.org")
        .Property(PropertyKey.ThrowOnUnsuccessfulStatusCode, false); 
    ```
- Automatic response deserialization to given types from XML, JSON, and pluggable custom representations
    ```cs
    // automatically mapped from JSON or XML depending on response content type, falling back to content sniffing
    MyClass response = await http.Target(url).Get<MyClass>();
    ```
- Rich exception class hierarchy
    ```cs
    try {
        string response = await http.Target(url).Get<string>();
    } catch(NotFoundException) {       // 404
    } catch(NotAuthorizedException) {  // 401
    } catch(ServerErrorException) {    // 5xx
    } catch(WebApplicationException) { // any unsuccessful status code
    } catch(ProcessingException) { }   // network or deserialization error, like DNS, connection refused, timeout
    ```
- Mockable and verifiable in unit or layer tests

### ICS
[![NuGet](https://img.shields.io/nuget/v/Unfucked.ICS?logo=nuget&label=Unfucked.ICS%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.ICS)
- For use with [Ical.Net](https://www.nuget.org/packages/TwitchApi.Net)
- Asynchronously serialize iCalendar files to a byte stream, to prevent Kestrel and IIS errors on synchronous I/O
- Converters between the three implementations of a datetime used by Ical.Net, NodaTime, and .NET

### OBS
[![NuGet](https://img.shields.io/nuget/v/Unfucked.OBS?logo=nuget&label=Unfucked.OBS%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.OBS)
- For use with [OBSClient](https://www.nuget.org/packages/OBSClient)
- Interface for Open Broadcaster Software Studio WebSocket API client to allow mocked testing isolation
- Easily connect and authenticate to servers without all the boilerplate

### PGP
[![NuGet](https://img.shields.io/nuget/v/Unfucked.PGP?logo=nuget&label=Unfucked.PGP%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.PGP)
- For use with [PgpCore](https://www.nuget.org/packages/PgpCore)
- Detached signing, where an ASCII-armored PGP signature is generated in a sidecar file, not inline wrapping the file being signed like in clearsigning

### STUN
[![NuGet](https://img.shields.io/nuget/v/Unfucked.STUN?logo=nuget&label=Unfucked.STUN%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.STUN)
- For use with [Stun.Net](https://www.nuget.org/packages/Stun.Net)
- STUN client that retains the server that it was configured with, useful for error logging
- Multi-server STUN client that transparently picks a random public STUN server from a constantly-updated list when sending a request, with retries and fallbacks if any of them fail
- Thread-safe multi-server STUN client that can be used concurrently from multiple threads without conflicts

### Twitch
[![NuGet](https://img.shields.io/nuget/v/Unfucked.Twitch?logo=nuget&label=Unfucked.Twitch%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.Twitch)
- For use with [TwitchApi.Net](https://www.nuget.org/packages/TwitchApi.Net)
- Interface for Twitch HTTP API client to allow mocked testing isolation

### Windows
[![NuGet](https://img.shields.io/nuget/v/Unfucked.Windows?logo=nuget&label=Unfucked.Windows%20on%20NuGet)](https://www.nuget.org/packages/Unfucked.Windows)
- For use with [Managed Windows API](https://mwinapi.sourceforge.net) ([mwinapi](https://www.nuget.org/packages/mwinapi))
- Reliably detect when computer is entering and exiting standby
- Kill the running screensaver
- Easier to get program's basename without memory leaks
- Get parent process of a process
- Get descendant processes recursively of a process
- Get locale of the operating system, rather than the user
- Convert between Win32 window handles, UI Automation elements, and mwinapi window instances
- Easily get all children of a UI Automation element
- Create a UI Automation property AND or OR condition that doesn't crash if there is only one sub-condition
- Find a child or descendant UI Automation element and wait if it doesn't immediately exist, instead of returning null, to prevent UI rendering race conditions
- Detach a console application from its console window if you want to prevent it from receiving `Ctrl`+`C`, because it's a child process of your console application, you're handling that signal in your parent process using [`Console.CancelKeyPress`](https://learn.microsoft.com/en-us/dotnet/api/system.console.cancelkeypress), and you don't want the console sidestepping your parent and killing your child.
   ```cs
   using Process child = Process.Start("child.exe", "args")!;
   child.DetachFromConsole();
   ```

## Related packages

#### Bom.Squad
[![NuGet](https://img.shields.io/nuget/v/Bom.Squad?logo=nuget&color=blue&label=Bom.Squad%20on%20NuGet)](https://www.nuget.org/packages/Bom.Squad)

Reconfigure the static shared `Encoding.UTF8` instance to not emit UTF-8 byte order marks to unbreak compatibility with non-Microsoft software.

#### DarkNet
[![NuGet](https://img.shields.io/nuget/v/DarkNet?logo=nuget&label=DarkNet%20on%20NuGet)](https://www.nuget.org/packages/DarkNet)

Turn the native Windows title bars dark in WPF and Windows Forms windows.

#### DataSizeUnits
[![NuGet](https://img.shields.io/nuget/v/DataSizeUnits?logo=nuget&label=DataSizeUnits%20on%20NuGet)](https://www.nuget.org/packages/DataSizeUnits)

Parse, convert, normalize, and format amounts of data like bits, bytes, kilobytes, and megabytes.

#### HidClient
[![NuGet](https://img.shields.io/nuget/v/HidClient?logo=nuget&label=HidClient%20on%20NuGet)](https://www.nuget.org/packages/HidClient)

Automatically connect and reconnect to USB HID peripherals.

#### KoKo
[![NuGet](https://img.shields.io/nuget/v/KoKo?logo=nuget&label=KoKo%20on%20NuGet)](https://www.nuget.org/packages/KoKo)

Automatically implement `INotifyPropertyChanged` to allow you to create composable properties with no publisher or subscriber boilerplate or circular references.

#### SolCalc
[![NuGet](https://img.shields.io/nuget/v/SolCalc?logo=nuget&label=SolCalc%20on%20NuGet)](https://www.nuget.org/packages/SolCalc)

Calculate a stream of solar time of day transitions for a given point on Earth, including sunrise, sunset, astronomical dawn and dusk, nautical dawn and dusk, and civil dawn and dusk.

#### ThrottleDebounce
[![NuGet](https://img.shields.io/nuget/v/ThrottleDebounce?logo=nuget&label=ThrottleDebounce%20on%20NuGet)](https://www.nuget.org/packages/ThrottleDebounce)

Throttle, debounce, and retry functions and actions.

#### UnionTypes
[![NuGet](https://img.shields.io/nuget/v/UnionTypes?logo=nuget&label=UnionTypes%20on%20NuGet)](https://www.nuget.org/packages/UnionTypes)

Implicitly cast multiple types to one union type, to avoid manually writing millions of method overloads.