🧰 Unfucked.DI
===

[![NuGet](https://img.shields.io/nuget/v/Unfucked.DI?logo=nuget&label=Package&color=informational)](https://www.nuget.org/packages/Unfucked.DI) [![GitHub Actions](https://img.shields.io/github/actions/workflow/status/Aldaviva/Unfucked/dotnetpackage.yml?branch=master&logo=github&label=Build)](https://github.com/Aldaviva/Unfucked/actions/workflows/dotnetpackage.yml) [![Testspace](https://img.shields.io/testspace/tests/Aldaviva/Aldaviva:Unfucked/master?passed_label=passing&failed_label=failing&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA4NTkgODYxIj48cGF0aCBkPSJtNTk4IDUxMy05NCA5NCAyOCAyNyA5NC05NC0yOC0yN3pNMzA2IDIyNmwtOTQgOTQgMjggMjggOTQtOTQtMjgtMjh6bS00NiAyODctMjcgMjcgOTQgOTQgMjctMjctOTQtOTR6bTI5My0yODctMjcgMjggOTQgOTQgMjctMjgtOTQtOTR6TTQzMiA4NjFjNDEuMzMgMCA3Ni44My0xNC42NyAxMDYuNS00NFM1ODMgNzUyIDU4MyA3MTBjMC00MS4zMy0xNC44My03Ni44My00NC41LTEwNi41UzQ3My4zMyA1NTkgNDMyIDU1OWMtNDIgMC03Ny42NyAxNC44My0xMDcgNDQuNXMtNDQgNjUuMTctNDQgMTA2LjVjMCA0MiAxNC42NyA3Ny42NyA0NCAxMDdzNjUgNDQgMTA3IDQ0em0wLTU1OWM0MS4zMyAwIDc2LjgzLTE0LjgzIDEwNi41LTQ0LjVTNTgzIDE5Mi4zMyA1ODMgMTUxYzAtNDItMTQuODMtNzcuNjctNDQuNS0xMDdTNDczLjMzIDAgNDMyIDBjLTQyIDAtNzcuNjcgMTQuNjctMTA3IDQ0cy00NCA2NS00NCAxMDdjMCA0MS4zMyAxNC42NyA3Ni44MyA0NCAxMDYuNVMzOTAgMzAyIDQzMiAzMDJ6bTI3NiAyODJjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjY3IDE0LjY3LTEwNiA0NHMtNDQgNjUtNDQgMTA3YzAgNDEuMzMgMTQuNjcgNzYuODMgNDQgMTA2LjVTNjY2LjY3IDU4NCA3MDggNTg0em0tNTU3IDBjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjgzIDE0LjY3LTEwNi41IDQ0UzAgMzkxIDAgNDMzYzAgNDEuMzMgMTQuODMgNzYuODMgNDQuNSAxMDYuNVMxMDkuNjcgNTg0IDE1MSA1ODR6IiBmaWxsPSIjZmZmIi8%2BPC9zdmc%2B&label=Tests)](https://aldaviva.testspace.com/spaces/285777) [![Coveralls](https://img.shields.io/coveralls/github/Aldaviva/Unfucked?logo=coveralls&label=Test%20Coverage)](https://coveralls.io/github/Aldaviva/Unfucked?branch=master)

*Fix egregiously missing or broken functionality in the dependency injection/inversion of control library [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting). Inspired by [Spring Framework](https://spring.io/projects/spring-framework/) and [JSR-330](https://jcp.org/en/jsr/detail?id=330).*

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3,4" -->

- [Installation](#installation)
- [Usage](#usage)
    - [Dependency Injection](#dependency-injection)
    - [Logging](#logging)
- [All Unfucked libraries](#all-unfucked-libraries)

<!-- /MarkdownTOC -->

[![Unfuck your house](https://raw.githubusercontent.com/Aldaviva/Unfucked/master/.github/images/frame.jpg)](https://loadingreadyrun.com/videos/view/2484/Crapshots-Ep270-The-Home-Show)

## Installation
```sh
dotnet add package Unfucked.DI
```
```cs
using Unfucked;
using Unfucked.DI;
```

## Usage

Create an application using [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/) or a [.NET Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host):

```cs
HostApplicationBuilder appBuilder = new HostApplicationBuilder(args);
using IHost app = appBuilder.Build();
await app.RunAsync();
```

### Dependency Injection

- In addition to searching for configuration JSON files in the current working directory, also search in the executable file's directory, so launching the program with a different CWD doesn't break configuration if that's where you're storing them.
    ```cs
    appBuilder.Configuration.AlsoSearchForJsonFilesInExecutableDirectory();
    ```
- Allow providers for a dependency service to be injected into a dependent instead of the service itself, which is useful when the dependent either has a longer lifetime than the dependency or needs to make multiple distinct instances of the dependency.
    ```cs
    appBuilder.Services
        .AddInjectableProviders()
        .AddSingleton<MyDependent>()
        .AddTransient<MyDependency>();

    class MyDependent(Provider<MyDependency> dependencyProvider){
        void Run(){
            using MyDependency dependency = dependencyProvider.Get();
        }
    }
    ```
- Change the exit code with which the program exits when a background service crashes, instead of 0 which incorrectly indicates success.
    ```cs
    appBuilder.Services.SetExitCodeOnBackgroundServiceException(1);
    ```
- Easily register a class in the DI context as itself and all of its interfaces and superclasses automatically, so you can inject it as any of the super types without any casting in constructors or unmaintainable multiple registration clutter.
    ```cs
    appBuilder.Services
        .AddSingleton<MyDependency>(alsoRegister: SuperRegistration.INTERFACES | SuperRegistration.SUPERCLASSES);

    class MyDependency: MyInterface;
    class MyDependent(MyInterface dependency);
    ```
    - Available for singletons, transients, scoped, hosted, and keyed services.

### Logging

- Add a colored console with single-line, column formatted data with simple type names.
    ```cs
    appBuilder.Logging.AddUnfuckedConsole();
    ```
- Increase the log message level of specified categories/classes and event IDs, in case the author of the original class foolishly logged important messages with at most the same level as lots of unimportant messages, so you can't just decrease your logger level filter for that entire class.
    ```cs
    appBuilder.Logging.AmplifyMessageLevels(options =>
        options.Amplify("Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher", LogLevel.Warning, 2, 3, 5, 11, 13, 14, 15, 19, 21, 22, 23, 24));
    ```
- Shorter alternatives for logging methods that aren't as ridiculously named as `LogInformation`, and that correctly format parameter values according to the current culture so you don't get fucked up percentages like `50 %`.
    ```cs
    appBuilder.Logging.AddUnfuckedConsole();

    ILogger<MyService> logger;
    logger.Info("Transfer {progress:P0} done", 0.5); // Transfer 50% done
    ```

## All Unfucked libraries
- [Unfucked](https://github.com/Aldaviva/Unfucked/tree/master/Unfucked)
- [Unfucked.Caching](https://github.com/Aldaviva/Unfucked/tree/master/Caching)
- [Unfucked.Compression](https://github.com/Aldaviva/Unfucked/tree/master/Compression)
- [Unfucked.DateTime](https://github.com/Aldaviva/Unfucked/tree/master/DateTime)
- **Unfucked.DI**
- [Unfucked.HTTP](https://github.com/Aldaviva/Unfucked/tree/master/HTTP)
- [Unfucked.ICS](https://github.com/Aldaviva/Unfucked/tree/master/ICS)
- [Unfucked.OBS](https://github.com/Aldaviva/Unfucked/tree/master/OBS)
- [Unfucked.PGP](https://github.com/Aldaviva/Unfucked/tree/master/PGP)
- [Unfucked.STUN](https://github.com/Aldaviva/Unfucked/tree/master/STUN)
- [Unfucked.Twitch](https://github.com/Aldaviva/Unfucked/tree/master/Twitch)
- [Unfucked.Windows](https://github.com/Aldaviva/Unfucked/tree/master/Windows)