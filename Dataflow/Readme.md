🧰 Unfucked.Dataflow
===

[![NuGet](https://img.shields.io/nuget/v/Unfucked.Dataflow?logo=nuget&label=package&color=informational)](https://www.nuget.org/packages/Unfucked.Dataflow) [![GitHub Actions](https://img.shields.io/github/actions/workflow/status/Aldaviva/Unfucked/dotnetpackage.yml?branch=master&logo=github&label=build)](https://github.com/Aldaviva/Unfucked/actions/workflows/dotnetpackage.yml) [![Testspace](https://img.shields.io/testspace/tests/Aldaviva/Aldaviva:Unfucked/master?passed_label=passing&failed_label=failing&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA4NTkgODYxIj48cGF0aCBkPSJtNTk4IDUxMy05NCA5NCAyOCAyNyA5NC05NC0yOC0yN3pNMzA2IDIyNmwtOTQgOTQgMjggMjggOTQtOTQtMjgtMjh6bS00NiAyODctMjcgMjcgOTQgOTQgMjctMjctOTQtOTR6bTI5My0yODctMjcgMjggOTQgOTQgMjctMjgtOTQtOTR6TTQzMiA4NjFjNDEuMzMgMCA3Ni44My0xNC42NyAxMDYuNS00NFM1ODMgNzUyIDU4MyA3MTBjMC00MS4zMy0xNC44My03Ni44My00NC41LTEwNi41UzQ3My4zMyA1NTkgNDMyIDU1OWMtNDIgMC03Ny42NyAxNC44My0xMDcgNDQuNXMtNDQgNjUuMTctNDQgMTA2LjVjMCA0MiAxNC42NyA3Ny42NyA0NCAxMDdzNjUgNDQgMTA3IDQ0em0wLTU1OWM0MS4zMyAwIDc2LjgzLTE0LjgzIDEwNi41LTQ0LjVTNTgzIDE5Mi4zMyA1ODMgMTUxYzAtNDItMTQuODMtNzcuNjctNDQuNS0xMDdTNDczLjMzIDAgNDMyIDBjLTQyIDAtNzcuNjcgMTQuNjctMTA3IDQ0cy00NCA2NS00NCAxMDdjMCA0MS4zMyAxNC42NyA3Ni44MyA0NCAxMDYuNVMzOTAgMzAyIDQzMiAzMDJ6bTI3NiAyODJjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjY3IDE0LjY3LTEwNiA0NHMtNDQgNjUtNDQgMTA3YzAgNDEuMzMgMTQuNjcgNzYuODMgNDQgMTA2LjVTNjY2LjY3IDU4NCA3MDggNTg0em0tNTU3IDBjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjgzIDE0LjY3LTEwNi41IDQ0UzAgMzkxIDAgNDMzYzAgNDEuMzMgMTQuODMgNzYuODMgNDQuNSAxMDYuNVMxMDkuNjcgNTg0IDE1MSA1ODR6IiBmaWxsPSIjZmZmIi8%2BPC9zdmc%2B&label=tests)](https://aldaviva.testspace.com/spaces/285777) [![Coveralls](https://img.shields.io/coveralls/github/Aldaviva/Unfucked?logo=coveralls&label=test%20coverage)](https://coveralls.io/github/Aldaviva/Unfucked?branch=master)

*Fix egregiously missing functionality in [System.Threading.Tasks.Dataflow](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/dataflow-task-parallel-library).*

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3,4" -->

- [Installation](#installation)
- [Usage](#usage)
- [All Unfucked libraries](#all-unfucked-libraries)

<!-- /MarkdownTOC -->

[![Unfuck your house](https://raw.githubusercontent.com/Aldaviva/Unfucked/master/.github/images/frame.jpg)](https://loadingreadyrun.com/videos/view/2484/Crapshots-Ep270-The-Home-Show)

## Installation
```sh
dotnet package add Unfucked.Dataflow
```
```cs
using Unfucked;
using Unfucked.Dataflow;
```

## Usage
- Simple aggregating [Dataflow](https://learn.microsoft.com/dotnet/standard/parallel-programming/dataflow-task-parallel-library) block that consumes all the items it's provided with until its linked source completes, then emits all consumed items in one big list (instead of one at a time like `BufferBlock`). Useful for joining/aggregating/reducing the stream of items so they can all be analyzed together, like finding the minimum item. Like a `BatchBlock` of unbounded size, in case you don't know how many producers there are, or to insulate your code from that changes to that quantity.
    ```cs
    CollectBlock<int> collectBlock = new();
    collectBlock.Post(1);
    collectBlock.Post(2);
    collectBlock.Post(3);
    collectBlock.Complete();
    IReadOnlyCollection<int> joined = collectBlock.Receive(); // [1, 2, 3]
    ```
- Easily create a block that produces and consumes items with custom logic, without all the boilerplate of `IPropagatorBlock` or the lack of reusability of `DataflowBlock.Encapsulate`. Just subclass and provide implementations of the `Input` and `Output` blocks. Useful when the built-in blocks almost do what you want, but not quite.
    ```cs
    public class CollectBlock<T>: InputOutputBlock<T, IReadOnlyCollection<T>> {

        protected sealed override ITargetBlock<T> Input { get; }
        protected override IReceivableSourceBlock<IReadOnlyCollection<T>> Output { get; }
        private readonly IList<T> items = [];

        public CollectBlock() {
            Input = new ActionBlock<T>(items.Add);
            BufferBlock<IReadOnlyCollection<T>> outputBuffer = new();
            Output = outputBuffer;

            Input.Completion.ContinueWith(_ => {
                if (items.Count != 0) {
                    outputBuffer.Post(items.AsReadOnlyList());
                }
                Output.Complete();
            });
        }
    }
    ```
- Fix the completion propagation defect where multiple source blocks link to the same target block, then one of the sources completes, causing the target to inadvertently complete early, instead of waiting for all the other sources to also complete. Like `Task.WhenAll` instead of `Task.WhenAny`. Useful when a block has multiple sources.
    ```cs
    ISourceBlock<int> source1, source2;
    ITargetBlock<int> target;
    PatientDataflowLinkOptions opts = new() {
        PropagateCompletion = true,
        PropagatingCompletionWaitsForAllSources = true
    };
    
    source1.LinkTo(target, opts);
    source2.LinkTo(target, opts);
    // target will complete when both source1 and source2 have completed, not just when one source has completed
    ```

## All Unfucked libraries
- [Unfucked](https://github.com/Aldaviva/Unfucked/tree/master/Unfucked)
- [Unfucked.Caching](https://github.com/Aldaviva/Unfucked/tree/master/Caching)
- [Unfucked.Compression](https://github.com/Aldaviva/Unfucked/tree/master/Compression)
- **Unfucked.Dataflow**
- [Unfucked.DateTime](https://github.com/Aldaviva/Unfucked/tree/master/DateTime)
- [Unfucked.DI](https://github.com/Aldaviva/Unfucked/tree/master/DI)
- [Unfucked.GitHub](https://github.com/Aldaviva/Unfucked/tree/master/GitHub)
- [Unfucked.HTTP](https://github.com/Aldaviva/Unfucked/tree/master/HTTP)
- [Unfucked.ICS](https://github.com/Aldaviva/Unfucked/tree/master/ICS)
- [Unfucked.OBS](https://github.com/Aldaviva/Unfucked/tree/master/OBS)
- [Unfucked.PGP](https://github.com/Aldaviva/Unfucked/tree/master/PGP)
- [Unfucked.STUN](https://github.com/Aldaviva/Unfucked/tree/master/STUN)
- [Unfucked.Twitch](https://github.com/Aldaviva/Unfucked/tree/master/Twitch)
- [Unfucked.Windows](https://github.com/Aldaviva/Unfucked/tree/master/Windows)