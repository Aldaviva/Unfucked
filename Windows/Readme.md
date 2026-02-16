🧰 Unfucked.Windows
===

[![NuGet](https://img.shields.io/nuget/v/Unfucked.Windows?logo=nuget&label=package&color=informational)](https://www.nuget.org/packages/Unfucked.Windows) [![GitHub Actions](https://img.shields.io/github/actions/workflow/status/Aldaviva/Unfucked/dotnetpackage.yml?branch=master&logo=github&label=build)](https://github.com/Aldaviva/Unfucked/actions/workflows/dotnetpackage.yml) [![Testspace](https://img.shields.io/testspace/tests/Aldaviva/Aldaviva:Unfucked/master?passed_label=passing&failed_label=failing&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA4NTkgODYxIj48cGF0aCBkPSJtNTk4IDUxMy05NCA5NCAyOCAyNyA5NC05NC0yOC0yN3pNMzA2IDIyNmwtOTQgOTQgMjggMjggOTQtOTQtMjgtMjh6bS00NiAyODctMjcgMjcgOTQgOTQgMjctMjctOTQtOTR6bTI5My0yODctMjcgMjggOTQgOTQgMjctMjgtOTQtOTR6TTQzMiA4NjFjNDEuMzMgMCA3Ni44My0xNC42NyAxMDYuNS00NFM1ODMgNzUyIDU4MyA3MTBjMC00MS4zMy0xNC44My03Ni44My00NC41LTEwNi41UzQ3My4zMyA1NTkgNDMyIDU1OWMtNDIgMC03Ny42NyAxNC44My0xMDcgNDQuNXMtNDQgNjUuMTctNDQgMTA2LjVjMCA0MiAxNC42NyA3Ny42NyA0NCAxMDdzNjUgNDQgMTA3IDQ0em0wLTU1OWM0MS4zMyAwIDc2LjgzLTE0LjgzIDEwNi41LTQ0LjVTNTgzIDE5Mi4zMyA1ODMgMTUxYzAtNDItMTQuODMtNzcuNjctNDQuNS0xMDdTNDczLjMzIDAgNDMyIDBjLTQyIDAtNzcuNjcgMTQuNjctMTA3IDQ0cy00NCA2NS00NCAxMDdjMCA0MS4zMyAxNC42NyA3Ni44MyA0NCAxMDYuNVMzOTAgMzAyIDQzMiAzMDJ6bTI3NiAyODJjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjY3IDE0LjY3LTEwNiA0NHMtNDQgNjUtNDQgMTA3YzAgNDEuMzMgMTQuNjcgNzYuODMgNDQgMTA2LjVTNjY2LjY3IDU4NCA3MDggNTg0em0tNTU3IDBjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjgzIDE0LjY3LTEwNi41IDQ0UzAgMzkxIDAgNDMzYzAgNDEuMzMgMTQuODMgNzYuODMgNDQuNSAxMDYuNVMxMDkuNjcgNTg0IDE1MSA1ODR6IiBmaWxsPSIjZmZmIi8%2BPC9zdmc%2B&label=tests)](https://aldaviva.testspace.com/spaces/285777) [![Coveralls](https://img.shields.io/coveralls/github/Aldaviva/Unfucked?logo=coveralls&label=test%20coverage)](https://coveralls.io/github/Aldaviva/Unfucked?branch=master)

*Fix egregiously missing or broken functionality in .NET libraries that integrate with Windows APIs.*

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3,4" -->

- [Installation](#installation)
- [Usage](#usage)
    - [Arguments](#arguments)
    - [Console](#console)
    - [Internationalization](#internationalization)
    - [Processes](#processes)
    - [Standby and screensaver](#standby-and-screensaver)
    - [UI Automation](#ui-automation)
- [All Unfucked libraries](#all-unfucked-libraries)

<!-- /MarkdownTOC -->

[![Unfuck your house](https://raw.githubusercontent.com/Aldaviva/Unfucked/master/.github/images/frame.jpg)](https://loadingreadyrun.com/videos/view/2484/Crapshots-Ep270-The-Home-Show)

## Installation
```sh
dotnet add package Unfucked.Windows
```
```cs
using Unfucked;
using Unfucked.Windows;
```

## Usage

### Arguments

- Command line argument marshalling with correct escaping and quoting
    - String to array
        ```cs
        IEnumerable<string> argv = WindowsProcesses.CommandLineToEnumerable("arg1 arg2");
        ```
    - Array to string
        ```cs
        string args = Processes.CommandLineToString(["arg1", "'argument' \"2\""]);
        ```

### Console

- Detach a console application from its console window if you want to prevent it from receiving `Ctrl`+`C`, because it's a child process of your console application, you're handling that signal in your parent process using [`Console.CancelKeyPress`](https://learn.microsoft.com/en-us/dotnet/api/system.console.cancelkeypress), and you don't want the console sidestepping your parent and killing your child.
   ```cs
   using Process child = Process.Start("child.exe", "args")!;
   child.DetachFromConsole();
   ```

### Internationalization

- Get locale of the operating system, rather than the user
    ```cs
    CultureInfo machineCulture = Cultures.CurrentMachineCulture;
    ```

### Processes

- Easier to get program's basename without memory leaks
    ```cs
    string? basename = SystemWindow.ForegroundWindow.GetProcessExecutableBasename();
    ```
- Get parent process of a process
    ```cs
    Process? parent = Process.GetCurrentProcess().GetParentProcess();
    ```
- Get descendant processes recursively of a process
    ```cs
    IEnumerable<Process> decendants = Process.GetCurrentProcess().GetDescendantProcesses();
    ```
- Detect if a process is suspended
    ```cs
    bool isSuspended = Process.GetProcessById(pid).IsProcessSuspended();
    ```
- Detect if a process is elevated (running as administrator)
    ```cs
    bool isElevated = Process.GetCurrentProcess().IsProcessElevated();
    ```
   
### Standby and screensaver

- Reliably detect when computer is entering and exiting standby
    ```cs
    using IStandbyListener standbyListener = new EventLogStandbyListener();
    standbyListener.StandingBy += (_, _) => Console.WriteLine("The computer is entering sleep mode");
    standbyListener.Resumed += (_, _) => Console.WriteLine("The computer woke up from sleep mode");
    ```
- Kill the running screensaver
    ```cs
    new ScreensaverKiller().KillScreensaver();
    ```

### UI Automation

- Convert between Win32 window handles, UI Automation elements, and mwinapi window instances
    ```cs
    AutomationElement? automationElement = SystemWindow.ForegroundWindow.ToAutomationElement();
    SystemWindow foreground = foregroundUiaElementd.ToSystemWindow();
    IntPtr? foregroundHwnd = automationElement.ToHwnd();
    ```
- Easily get all children of a UI Automation element
    ```cs
    IEnumerable<AutomationElement> children = automationElement.Children();
    ```
- Create a UI Automation property AND or OR condition that doesn't crash if there is only one sub-condition
    ```cs
    IEnumerable<string> allowedNames = ["A"];
    automationElement.FindFirst(TreeScope.Children, UIAutomationExtensions.SingletonSafePropertyCondition(AutomationElement.NameProperty, false, allowedNames));
    ```
- Find a child or descendant UI Automation element and wait if it doesn't immediately exist, instead of returning null, to prevent UI rendering race conditions
    ```cs
    AutomationElement? a = automationElement.WaitForFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "A"), TimeSpan.FromSeconds(30), cancellationToken);
    ```


## All Unfucked libraries
- [Unfucked](https://github.com/Aldaviva/Unfucked/tree/master/Unfucked)
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
- **Unfucked.Windows**