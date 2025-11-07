🧰 Unfucked.Compression
===

[![NuGet](https://img.shields.io/nuget/v/Unfucked.Compression?logo=nuget&label=package&color=informational)](https://www.nuget.org/packages/Unfucked.Compression) [![GitHub Actions](https://img.shields.io/github/actions/workflow/status/Aldaviva/Unfucked/dotnetpackage.yml?branch=master&logo=github&label=build)](https://github.com/Aldaviva/Unfucked/actions/workflows/dotnetpackage.yml) [![Testspace](https://img.shields.io/testspace/tests/Aldaviva/Aldaviva:Unfucked/master?passed_label=passing&failed_label=failing&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA4NTkgODYxIj48cGF0aCBkPSJtNTk4IDUxMy05NCA5NCAyOCAyNyA5NC05NC0yOC0yN3pNMzA2IDIyNmwtOTQgOTQgMjggMjggOTQtOTQtMjgtMjh6bS00NiAyODctMjcgMjcgOTQgOTQgMjctMjctOTQtOTR6bTI5My0yODctMjcgMjggOTQgOTQgMjctMjgtOTQtOTR6TTQzMiA4NjFjNDEuMzMgMCA3Ni44My0xNC42NyAxMDYuNS00NFM1ODMgNzUyIDU4MyA3MTBjMC00MS4zMy0xNC44My03Ni44My00NC41LTEwNi41UzQ3My4zMyA1NTkgNDMyIDU1OWMtNDIgMC03Ny42NyAxNC44My0xMDcgNDQuNXMtNDQgNjUuMTctNDQgMTA2LjVjMCA0MiAxNC42NyA3Ny42NyA0NCAxMDdzNjUgNDQgMTA3IDQ0em0wLTU1OWM0MS4zMyAwIDc2LjgzLTE0LjgzIDEwNi41LTQ0LjVTNTgzIDE5Mi4zMyA1ODMgMTUxYzAtNDItMTQuODMtNzcuNjctNDQuNS0xMDdTNDczLjMzIDAgNDMyIDBjLTQyIDAtNzcuNjcgMTQuNjctMTA3IDQ0cy00NCA2NS00NCAxMDdjMCA0MS4zMyAxNC42NyA3Ni44MyA0NCAxMDYuNVMzOTAgMzAyIDQzMiAzMDJ6bTI3NiAyODJjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjY3IDE0LjY3LTEwNiA0NHMtNDQgNjUtNDQgMTA3YzAgNDEuMzMgMTQuNjcgNzYuODMgNDQgMTA2LjVTNjY2LjY3IDU4NCA3MDggNTg0em0tNTU3IDBjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjgzIDE0LjY3LTEwNi41IDQ0UzAgMzkxIDAgNDMzYzAgNDEuMzMgMTQuODMgNzYuODMgNDQuNSAxMDYuNVMxMDkuNjcgNTg0IDE1MSA1ODR6IiBmaWxsPSIjZmZmIi8%2BPC9zdmc%2B&label=tests)](https://aldaviva.testspace.com/spaces/285777) [![Coveralls](https://img.shields.io/coveralls/github/Aldaviva/Unfucked?logo=coveralls&label=test%20coverage)](https://coveralls.io/github/Aldaviva/Unfucked?branch=master)

*Fix egregiously missing functionality in [SharpCompress](https://www.nuget.org/packages/SharpCompress).*

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3,4" -->

- [Installation](#installation)
- [Usage](#usage)
    - [Get started](#get-started)
    - [Set file or directory metadata](#set-file-or-directory-metadata)
        - [Mode](#mode)
        - [Owner](#owner)
        - [Group](#group)
    - [Add symbolic link](#add-symbolic-link)
- [All Unfucked libraries](#all-unfucked-libraries)

<!-- /MarkdownTOC -->

[![Unfuck your house](https://raw.githubusercontent.com/Aldaviva/Unfucked/master/.github/images/frame.jpg)](https://loadingreadyrun.com/videos/view/2484/Crapshots-Ep270-The-Home-Show)

## Installation
```sh
dotnet add package Unfucked.Compression
```
```cs
using TarWriter = Unfucked.Compression.Writers.Tar.TarWriter;
```

## Usage

### Get started
```cs
await using FileStream outputFileStream;
await using GZipStream gz = new(outputFileStream, CompressionMode.Compress, CompressionLevel.Default);
using TarWriter tar = new TarWriter(gz, new TarWriterOptions(CompressionType.None, true));
```

### Set file or directory metadata

#### Mode
```cs
tar.WriteFile(filename: "file.txt", source: inputStream, fileMode: Convert.ToInt32("600", 8));
```
```cs
tar.WriteDirectory(directoryName: "dir", fileMode: Convert.ToInt32("600", 8));
```

#### Owner
```cs
tar.WriteFile(filename: "file.txt", source: inputStream, ownerId: 1000);
```
```cs
tar.WriteDirectory(directoryName: "dir", ownerId: 1000);
```

#### Group
```cs
tar.WriteFile(filename: "file.txt", source: inputStream, groupId: 1000);
```
```cs
tar.WriteDirectory(directoryName: "dir", groupId: 1000);
```

### Add symbolic link

```cs
tar.WriteSymLink(source: "./mylink.txt", destination: "./file.txt", DateTime.Now, ownerId: 1000, groupId: 1000);
```

## All Unfucked libraries
- [Unfucked](https://github.com/Aldaviva/Unfucked/tree/master/Unfucked)
- [Unfucked.Caching](https://github.com/Aldaviva/Unfucked/tree/master/Caching)
- **Unfucked.Compression**
- [Unfucked.DateTime](https://github.com/Aldaviva/Unfucked/tree/master/DateTime)
- [Unfucked.DI](https://github.com/Aldaviva/Unfucked/tree/master/DI)
- [Unfucked.HTTP](https://github.com/Aldaviva/Unfucked/tree/master/HTTP)
- [Unfucked.ICS](https://github.com/Aldaviva/Unfucked/tree/master/ICS)
- [Unfucked.OBS](https://github.com/Aldaviva/Unfucked/tree/master/OBS)
- [Unfucked.PGP](https://github.com/Aldaviva/Unfucked/tree/master/PGP)
- [Unfucked.STUN](https://github.com/Aldaviva/Unfucked/tree/master/STUN)
- [Unfucked.Twitch](https://github.com/Aldaviva/Unfucked/tree/master/Twitch)
- [Unfucked.Windows](https://github.com/Aldaviva/Unfucked/tree/master/Windows)