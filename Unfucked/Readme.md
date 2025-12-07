🧰 Unfucked
===

[![NuGet](https://img.shields.io/nuget/v/Unfucked?logo=nuget&label=package&color=informational)](https://www.nuget.org/packages/Unfucked) [![GitHub Actions](https://img.shields.io/github/actions/workflow/status/Aldaviva/Unfucked/dotnetpackage.yml?branch=master&logo=github&label=build)](https://github.com/Aldaviva/Unfucked/actions/workflows/dotnetpackage.yml) [![Testspace](https://img.shields.io/testspace/tests/Aldaviva/Aldaviva:Unfucked/master?passed_label=passing&failed_label=failing&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA4NTkgODYxIj48cGF0aCBkPSJtNTk4IDUxMy05NCA5NCAyOCAyNyA5NC05NC0yOC0yN3pNMzA2IDIyNmwtOTQgOTQgMjggMjggOTQtOTQtMjgtMjh6bS00NiAyODctMjcgMjcgOTQgOTQgMjctMjctOTQtOTR6bTI5My0yODctMjcgMjggOTQgOTQgMjctMjgtOTQtOTR6TTQzMiA4NjFjNDEuMzMgMCA3Ni44My0xNC42NyAxMDYuNS00NFM1ODMgNzUyIDU4MyA3MTBjMC00MS4zMy0xNC44My03Ni44My00NC41LTEwNi41UzQ3My4zMyA1NTkgNDMyIDU1OWMtNDIgMC03Ny42NyAxNC44My0xMDcgNDQuNXMtNDQgNjUuMTctNDQgMTA2LjVjMCA0MiAxNC42NyA3Ny42NyA0NCAxMDdzNjUgNDQgMTA3IDQ0em0wLTU1OWM0MS4zMyAwIDc2LjgzLTE0LjgzIDEwNi41LTQ0LjVTNTgzIDE5Mi4zMyA1ODMgMTUxYzAtNDItMTQuODMtNzcuNjctNDQuNS0xMDdTNDczLjMzIDAgNDMyIDBjLTQyIDAtNzcuNjcgMTQuNjctMTA3IDQ0cy00NCA2NS00NCAxMDdjMCA0MS4zMyAxNC42NyA3Ni44MyA0NCAxMDYuNVMzOTAgMzAyIDQzMiAzMDJ6bTI3NiAyODJjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjY3IDE0LjY3LTEwNiA0NHMtNDQgNjUtNDQgMTA3YzAgNDEuMzMgMTQuNjcgNzYuODMgNDQgMTA2LjVTNjY2LjY3IDU4NCA3MDggNTg0em0tNTU3IDBjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjgzIDE0LjY3LTEwNi41IDQ0UzAgMzkxIDAgNDMzYzAgNDEuMzMgMTQuODMgNzYuODMgNDQuNSAxMDYuNVMxMDkuNjcgNTg0IDE1MSA1ODR6IiBmaWxsPSIjZmZmIi8%2BPC9zdmc%2B&label=tests)](https://aldaviva.testspace.com/spaces/285777) [![Coveralls](https://img.shields.io/coveralls/github/Aldaviva/Unfucked?logo=coveralls&label=test%20coverage)](https://coveralls.io/github/Aldaviva/Unfucked?branch=master)

*Fix egregiously broken or missing functionality in .NET libraries. Provide useful boilerplate, façades, and polyfills. Inspired by [underscore](https://underscorejs.org), [lodash](https://lodash.com), [Apache Commons](https://commons.apache.org), [Spring](https://spring.io/projects/spring-framework), [Guava](https://guava.dev), [jQuery](https://jquery.com),[Dojo](https://dojotoolkit.org), [mootools](https://mootools.net), and [Prototype](http://prototypejs.org).*

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3,4" -->

- [Installation](#installation)
- [Usage](#usage)
    - [Comparables](#comparables)
    - [Console](#console)
    - [Cryptography](#cryptography)
    - [Date and Time](#date-and-time)
    - [Decimal math](#decimal-math)
    - [Directories](#directories)
    - [DNS](#dns)
    - [Enumerables](#enumerables)
    - [Exceptions](#exceptions)
    - [Lazy](#lazy)
    - [Paths](#paths)
    - [Processes](#processes)
    - [Strings](#strings)
    - [Tasks](#tasks)
    - [URIs](#uris)
    - [XML](#xml)
- [All Unfucked libraries](#all-unfucked-libraries)

<!-- /MarkdownTOC -->

[![Unfuck your house](https://raw.githubusercontent.com/Aldaviva/Unfucked/master/.github/images/frame.jpg)](https://loadingreadyrun.com/videos/view/2484/Crapshots-Ep270-The-Home-Show)

## Installation
```sh
dotnet add package Unfucked
```
```cs
using Unfucked;
```

## Usage

### Comparables
- Clip a value to a specified range (also known as clamping, limiting, and truncating).
    ```cs
    int health = health.Clip(min: 0, max: 100);
    ```

### Console
- Colored text and background in strings, not tightly coupled to `Console.Write`.
    ```cs
    string colored = ConsoleControl.Color("Hello", Color.FromArgb(0xff, 0xaa, 0), Color.Black);
    ConsoleControl.WriteLine("Hello", Color.FromArgb(0xff, 0xaa, 0), Color.Black);
    ConsoleControl.Write("Hello", Color.FromArgb(0xff, 0xaa, 0), Color.Black);
    ```
- Clear screen and move cursor.
    ```cs
    ConsoleControl.Clear();     // clear screen and move to top-left corner
    ConsoleControl.ClearLine(); // clear line and move to left side
    ```
- Enable colored output on Windows 10 1511 and later.
    - Called implicitly whenever you call the `ConsoleControl.Color`, `Write`, `WriteLine`, `Clear`, or `ClearLine` methods.
    - Can be manually checked and opportunistically enabled.
        ```cs
        bool colorable = ConsoleControl.IsColorSupported();
        ```

### Cryptography
- Random string generation
    ```cs
    string randomString = Cryptography.GenerateRandomString(length: 20);
    ```
    - *[Polyfill for .NET < 8](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.getstring)*
- Is certificate temporally valid?
    ```cs
    X509Certificate2 myCert;
    bool isValid = myCert.IsTemporallyValid(safetyMargin: TimeSpan.Zero, now: DateTime.Now);
    ```
    > [!WARNING]
    > Only checks time validity, not trust chain, revocation, or algorithm weakness.
- Get certificate subject/issuer named part.
    ```cs
    X509Certificate2 myCert;
    string? subjectCommonName = myCert.SubjectName.Get("CN");
    string? issuerOrg = myCert.Issuer.Get("O");
    ```


### Date and Time
- Absolute value of `TimeSpan` with a more discoverable name that `Duration`, whose name doesn't imply absolute value at all
    ```cs
    TimeSpan absValue = TimeSpan.FromHours(-1).Abs() == TimeSpan.FromHours(1);
    ```

### Decimal math
- `Math` [operations](https://github.com/raminrahimzada/CSharp-Helper-Classes/tree/8f77a2b048a598d02053b7118a7fd63edf6c99cd/Math/DecimalMath) on 128-bit `decimal` values
    - `ATan`
    - `Acos`
    - `Asin`
    - `Atan2`
    - `CalculateSinFromCos`
    - `Cos`
    - `Cosh`
    - `Exp`
    - `IsInteger`
    - `IsSignOfSinePositive`
    - `Log`
    - `Log10`
    - `Power`
    - `PowerN`
    - `Sin`
    - `Sinh`
    - `Sqrt`
    - `Tan`
    - `Tanh`
    - `TruncateToPeriodicInterval`

### Directories
- Delete directory without throwing an exception on missing directories.
    ```cs
    bool found = Directories.TryDelete(directory: "myDir", recursive: false);
    ```

### DNS
- Fluent resolving method
    ```cs
    IPEndPoint? ipAddress = await new DnsEndPoint("aldaviva.com", 443).Resolve();
    ```
    - *If you need to resolve anything other than an A or AAAA record, take a look at [DnsClient.NET](https://www.nuget.org/packages/DnsClient) instead*

### Enumerables
- Filter out `null` values.
    ```cs
    IEnumerable<string?> sparseList = new List<string?> { "a", "b", null };
    IEnumerable<string> list = withNulls.Compact(); // ["a", "b"]

    IDictionary<string, object?> sparseMap = new Dictionary<string, object?> {
        ["a"] = "AA",
        ["b"] = "BB",
        ["c"] = null
    };
    IDictionary<string, object> map = sparseMap.Compact(); // { "a": "AA", "b": "BB" }
    ```
- Add multiple values at once.
    ```cs
    IList<string> list = ["a"];
    list.AddAll("b", "c"); // ["a", "b", "c"]
    ```
- Upsert into non-concurrent dictionary.
    ```cs
    Dictionary<string, string> map = new() {
        ["a"] = "AA"
    };

    string a = map.GetOrAdd(key: "a", value: "AA", out bool aAdded); // a == "AA", aAdded == false
    string b = map.GetOrAdd(key: "b", valueFactory: () => "BB", out bool bAdded); // b == "BB", bAdded == true
    ```
    - Value factories can also be asynchronous
- Fluent set difference method
    ```cs
    ISet<string> original = new HashSet<string> { "a", "b", "c" };
    ISet<string> subtract = new HashSet<string> { "a", "b" };
    ISet<string> diff = original.Minus(subtract); // ["CC"]
    ```
- Filter out consecutive duplicate values.
    ```cs
    IEnumerable<string> original = ["a", "a", "b", "b", "c", "b"];
    IEnumerable<string> deduped = original.DistinctConsecutive(); // ["a", "b", "c", "b"]
    ```
- Convert `IAsyncEnumerator` to list.
    ```cs
    IAsyncEnumerator<string> enumerator = MyEnumerateAsync();
    IReadOnlyList<string> enumerated = await enumerator.ToList();
    ```
- Get the first, last, singleton, or indexed item, or return `null` instead of `default` for value types, which are ambiguous with present elements.
    ```cs
    IEnumerable<TimeSpan> ts = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)];
    TimeSpan? head     = ts.FirstOrNull();                                          // 1 second
    TimeSpan? last     = ts.LastOrNull();                                           // 2 seconds
    TimeSpan singleton = ts.SingleOrNull() ?? TimeSpan.FromSeconds(3);              // 3 seconds
    TimeSpan t         = ts.ElementAtOrNull(index: 100) ?? TimeSpan.FromSeconds(4); // 4 seconds
    ```
- Get an element from a dictionary by key, or return `null` instead of `default`, because default value types are ambiguous with present elements. This avoids having to use an awkward, verbose TryGetValue in a ternary operator every time. Also includes methods for reference typed keys, because `TryGetValue` did not exist in .NET Standard 2.0.
    ```cs
    var structDict = new Dictionary<string, int> { { "a", 0 } };
    int? a = structDict.GetValueOrNullStruct("a"); // 0
    int? b = structDict.GetValueOrNullStruct("b"); // null instead of 0, the default value of int, which is ambiguous with the presence of "b"

    var classDict = new Dictionary<string, string> { { "c", "0" } };
    string? c = classDict.GetValueOrNull("c"); // "0"
    string? d = classDict.GetValueOrNull("d"); // null
    ```
- Head and tail
    ```cs
    IEnumerable<string> original = ["a", "b", "c"];
    (string? head, IEnumerable<string> tail) = original.HeadAndTail();
    // head == "a", tail == ["b", "c"]
    ```
    - If the item type is a value-type struct, use `HeadAndTailStruct()` instead.
- Delta changeset between two enumerables (created, updated, deleted, and unchanged items)
    ```cs
    public record Person(long id, string name);
    IEnumerable<string> original = [new Person(1, "Alice"), new Person(2, "Bob"), new Person(3, "Charlie")];
    IEnumerable<string> @new = [new Person(1, "Alice"), new Person(3, "Carol"), new Person(4, "Dan")];

    (IEnumerable<Person> created, IEnumerable<Person> updated, IEnumerable<Person> deleted, IEnumerable<Person> unmodified) 
        = original.DeltaWith(@new, person => person.id);
    // created contains Dan
    // updated contains Carol
    // deleted contains Bob
    // unmodified contains Alice
    ```
    - The original and new items don't have to be of the same type.
- Atomic swap on `ConcurrentDictionary` to upsert new value and get old value
    ```cs
    ConcurrentDictionary<string, ValueHolder<int>> map = Enumerables.CreateConcurrentDictionary(Singleton.Dictionary("a", 1));
    int? oldValue = map.Swap(key: "a", newValue: 2); // oldValue == 1, map["a"] == 2
    ```
- Atomic compare and swap on `ConcurrentDictionary` to update new value if it exists and matches old value, and also get the old value
    ```cs
    ConcurrentDictionary<string, ValueHolder<int>> map = Enumerables.CreateConcurrentDictionary(Singleton.Dictionary("a", 1));
    int? oldValue = map.CompareAndSwap(key: "a", oldValue: 1, newValue: 2);
    ```
- Get or add to `ConcurrentDictionary` and determine whether a new value was added or an existing value was returned
    ```cs
    ConcurrentDictionary<long, Person> clients = new();
    Person alice = clients.GetOrAdd(key: 1, newValue: new Person("Alice"), out bool aliceAdded);
    Person bob   = clients.GetOrAdd(key: 2, valueFactory: id => new Person(id, "Bob"), out bool bobAdded);
    ```
- Get or add to `ConcurrentDictionary` and dispose of created but unadded values.
    ```cs
    ConcurrentDictionary<string, HttpClient> clients = new();
    HttpClient actual = clients.GetOrAddWithDisposal(key: "aldaviva.com", valueFactory: domain => new HttpClient { BaseAddress = domain }, out bool added);
    // if clients already contained the key "aldaviva.com", then
    //     actual will be the existing client,
    //     added will be false, and
    //     any new HttpClient that may have been temporarily created for insertion before being discarded will be disposed
    // otherwise, actual will be a new HttpClient instance, and added will be true
    ```
- Factories for singleton Dictionaries, Sets, and enumerables of key-value pairs
    ```cs
    IReadOnlyDictionary<string, int>       dict = Singleton.Dictionary(key: "a", value: 1);
    IReadOnlySet<string>                   set  = Singleton.Set(item: "a");
    IEnumerable<KeyValuePair<string, int>> kvs  = Singleton.KeyValues(key: "a", value: 1);
    ```
- Filter by exact type instead of [by superclass](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.oftype).
    ```cs
    public class Superclass;
    public class Subclass: Superclass;

    IEnumerable<object> a = [new Superclass(), new Subclass()];
    IEnumerable<Superclass> superclasses = a.OfTypeExactly<Superclass>();
    // superclasses does not contain any Subclass instances
    ```
- Polyfill for [`IList<T>.AsReadOnly`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.collectionextensions.asreadonly#system-collections-generic-collectionextensions-asreadonly-1(system-collections-generic-ilist((-0)))) for .NET versions before 8, including .NET Standard
    ```cs
    IReadOnlyList<string> ro = new List<string> { "a", "b" }.AsReadOnly();
    ```

### Exceptions
- Get the chain of causes for an exception, which is a sequence of all of its inner exceptions, recursively. Excludes outermost exception.
    ```cs
    Exception e;
    IEnumerable<Exception> causeChain = e.GetCauseChain();
    // equivalent to [e.InnerException, e.InnerException.InnerException, ...]
    ```
- Get the chain of messages for an exception, which is a string of all of its inner messages, recursively. Includes outermost message.
    ```cs
    Exception e;
    string messages = e.MessageChain(includeClassNames: true);
    // like $"Exception: outer message; OtherExceptionClass: inner message; ..."
    ```
- Determine if an `IOException` was caused by a file already existing on Windows.
    ```cs
    try {
        Stream file = new FileStream("filename", FileMode.CreateNew, FileAccess.Write);
    } catch (IOException e) when (e.IsCausedByExistingWindowsFile()){
        // filename already exists
    }
    ```
    - This case is undetectable on Linux and Mac OS, so it always returns `false` there.

### Lazy
- Easily dispose of lazy value without all the conditional and exception handling boilerplate
    ```cs
    Lazy<HttpClient> httpClientHolder = new(() => new HttpClient(), LazyThreadSafetyMode.PublicationOnly);
    httpClientHolder.TryDisposeValue();
    ```

### Paths
- Trim trailing slashes
    ```cs
    Paths.TrimTrailingSlashes(@"C:\Users\Ben\Desktop\") == @"C:\Users\Ben\Desktop"
    Paths.TrimTrailingSlashes("/home/ben/") == "/home/ben"
    ```
- Create new empty temporary subdirectory in specific parent directory
    ```cs
    Paths.CreateTempDir(); // example: %LOCALAPPDATA%\Temp\temp-12345678
    Paths.CreateTempDir(Environment.ExpandEnvironmentVariables(@"%temp%\myapp")); // example: %LOCALAPPDATA%\Temp\myapp\temp-abcdefgh
    ```
- Convert DOS backslashes to Unix forward slashes
    ```cs
    Paths.Dos2UnixSlashes(@"C:\Users\Ben\Desktop") == "C:/Users/Ben/Desktop"
    ```
- Match file extension against a set
    ```cs
    IReadOnlySet<string> videoExtensions { get; } = new HashSet<string> {
           ".3gpp", ".asf", ".avi", ".bik", ".divx", ".dv", ".f4v", ".flv", ".m1v", ".m4v", ".mkv", ".mov", ".mp4", ".mp4v", ".mpeg", ".mpg", ".ts", ".vob", ".webm", ".wmv"}.ToFrozenSet();
    bool hasVideoExt = Paths.MatchesExtensions("myfile.mp4", videoExtensions);
    ```

### Processes
- Command line argument marshalling with correct escaping and quoting
    - String to array (requires [`Unfucked.Windows`](#windows) package)
        ```cs
        IEnumerable<string> argv = WindowsProcesses.CommandLineToEnumerable("arg1 arg2");
        ```
    - Array to string
        ```cs
        string args = Processes.CommandLineToString(["arg1", "'argument' \"2\""]);
        ```
- Run program and get output and exit code, like [Node.js' `child_process.execFile()`](https://nodejs.org/api/child_process.html#child_processexecfilefile-args-options-callback)
    ```cs
    (int exitCode, string stdout, string stderr) result = 
        await Processes.ExecFile("path/to/program.exe", ["arg1", "arg2"], extraEnv, "workDir", hideWindow: false, ct);
    ```
- Determine whether the current program is a console or Windows GUI app.
    ```cs
    bool isWindowsGuiProgram = Processes.IsWindowsGuiProgram();
    ```

### Strings
- Coerce empty strings to `null`
    ```cs
    "".EmptyToNull(); // null
    ```
- Fluently check if a string has any non-whitespace characters
    ```cs
    " ".HasText(); // false
    ```
- Fluently check if a string has any characters
    ```cs
    "".HasLength(); // false
    ```
- Fluent join method
    ```cs
    new[] { "A", "B" }.Join(", ");
    ```
- Uppercase first letter
    ```cs
    "ben".ToUpperFirstLetter(); // "Ben"
    ```
- Lowercase first letter
    ```cs
    "Ben".ToLowerFirstLetter(); // "ben"
    ```
- Trim multiple strings from start, end, or both
    ```cs
    "..::Ben::..".Trim(".", ":"); // "Ben"
    ```
- Join enumerable into an English-style list with commas an a conjunction like `and`
    ```cs
    new[] { "Dewey", "Cheetum", "Howe" }.JoinHumanized(",", "and", true); // "Dewey, Cheetum, and Howe"
    ```
- Fluent conversion method to byte array
    ```cs
    "Ben".ToByteArray(new UTF8Encoding(false, true)); // [0x42, 0x65, 0x6E]
    ```
- Fluent conversion method to byte stream
    ```cs
    Stream stream = "Ben".ToByteStream();
    ```
- Convert DOS CRLF line breaks to Unix LF line breaks
    ```cs
    "A\r\nB".Dos2Unix(); // "A\nB"        
    ```
- Repeat a string a certain number of times
    ```cs
    "Ben".Repeat(4); // "BenBenBenBen"
    ```
- Polyfills for [`StringBuilder.AppendJoin`](https://learn.microsoft.com/en-us/dotnet/api/system.text.stringbuilder.appendjoin) in .NET Standard 2.0
- Polyfills for [`string.StartsWith(char)`](https://learn.microsoft.com/en-us/dotnet/api/system.string.startswith#system-string-startswith(system-char)) and [`string.EndsWith(char)`](https://learn.microsoft.com/en-us/dotnet/api/system.string.endswith#system-string-endswith(system-char)) in .NET Standard 2.0
- Polyfills for [`string.Contains(string, StringComparison)`](https://learn.microsoft.com/en-us/dotnet/api/system.string.contains#system-string-contains(system-string-system-stringcomparison)) in .NET Standard 2.0
- Polyfills for [`string.Join`](https://learn.microsoft.com/en-us/dotnet/api/system.string.join?view=net-9.0#system-string-join(system-string-system-readonlyspan((system-string)))) overloads that take `ReadOnlySpan` in .NET Standard 2.0 and .NET Runtimes < 9

### Tasks
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
- Easily await tasks that may be null without having to add parentheses and null coalescing operators.
    ```cs
    Task? myOptionalTask = null;
    await myOptionalTask.CompleteIfNull();
    ```

### URIs
- Fluent method to get URL query parameters
    ```cs
    string? value = new Uri("https://aldaviva.com?key=value").GetQuery()["key"];
    ```
- Builder pattern for URLs
    ```cs
    Uri url = new UrlBuilder("https", "aldaviva.com")
        .Path("a")
        .Path("b/c")
        .QueryParam("d", "e")
        .QueryParam("f", "{f}")
        .ResolveTemplate("f", "g")
        .Fragment("h")
        .ToUrl(); // https://aldaviva.com/a/b/c?d=e&f=g#h
    ```
- Truncate URIs to remove the fragment, query parameters, or path. Useful for getting the origin too.
    ```cs
    Uri full = new("https://ben@aldaviva.com:443/path?key=value#hash");
    full.Truncate(URI.Part.Query);     // https://ben@aldaviva.com:443/path?key=value
    full.Truncate(URI.Part.Path);      // https://ben@aldaviva.com:443/path
    full.Truncate(URI.Part.Authority); // https://ben@aldaviva.com:443/
    full.Truncate(URI.Part.Origin);    // https://aldaviva.com:443
    ```

### XML
- Fluent methods to read an XML document from an HTTP response body as a mapped object, DOM, LINQ, or XPath
    ```cs
    using HttpResponseMessage response = await new HttpClient().GetAsync(url);
    MyClass        obj   = await response.Content.ReadObjectFromXmlAsync<MyClass>();
    XmlDocument    dom   = await response.Content.ReadDomFromXmlAsync();
    XDocument      linq  = await response.Content.ReadLinqFromXmlAsync();
    XPathNavigator xpath = await response.Content.ReadXPathFromXmlAsync();
    ```
- Find all descendant elements of a parent node which have a given tag name.
    ```cs
    XDocument doc = XDocument.Parse(xmlString);
    IEnumerable<XElement> els = doc.Descendants("head", "body");
    ```

## All Unfucked libraries
- **Unfucked**
- [Unfucked.Caching](https://github.com/Aldaviva/Unfucked/tree/master/Caching)
- [Unfucked.Compression](https://github.com/Aldaviva/Unfucked/tree/master/Compression)
- [Unfucked.DateTime](https://github.com/Aldaviva/Unfucked/tree/master/DateTime)
- [Unfucked.DI](https://github.com/Aldaviva/Unfucked/tree/master/DI)
- [Unfucked.HTTP](https://github.com/Aldaviva/Unfucked/tree/master/HTTP)
- [Unfucked.ICS](https://github.com/Aldaviva/Unfucked/tree/master/ICS)
- [Unfucked.OBS](https://github.com/Aldaviva/Unfucked/tree/master/OBS)
- [Unfucked.PGP](https://github.com/Aldaviva/Unfucked/tree/master/PGP)
- [Unfucked.STUN](https://github.com/Aldaviva/Unfucked/tree/master/STUN)
- [Unfucked.Twitch](https://github.com/Aldaviva/Unfucked/tree/master/Twitch)
- [Unfucked.Windows](https://github.com/Aldaviva/Unfucked/tree/master/Windows)