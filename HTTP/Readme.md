🧰 Unfucked.HTTP
===

[![NuGet](https://img.shields.io/nuget/v/Unfucked.HTTP?logo=nuget&label=package&color=informational)](https://www.nuget.org/packages/Unfucked.HTTP) [![GitHub Actions](https://img.shields.io/github/actions/workflow/status/Aldaviva/Unfucked/dotnetpackage.yml?branch=master&logo=github&label=build)](https://github.com/Aldaviva/Unfucked/actions/workflows/dotnetpackage.yml) [![Testspace](https://img.shields.io/testspace/tests/Aldaviva/Aldaviva:Unfucked/master?passed_label=passing&failed_label=failing&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA4NTkgODYxIj48cGF0aCBkPSJtNTk4IDUxMy05NCA5NCAyOCAyNyA5NC05NC0yOC0yN3pNMzA2IDIyNmwtOTQgOTQgMjggMjggOTQtOTQtMjgtMjh6bS00NiAyODctMjcgMjcgOTQgOTQgMjctMjctOTQtOTR6bTI5My0yODctMjcgMjggOTQgOTQgMjctMjgtOTQtOTR6TTQzMiA4NjFjNDEuMzMgMCA3Ni44My0xNC42NyAxMDYuNS00NFM1ODMgNzUyIDU4MyA3MTBjMC00MS4zMy0xNC44My03Ni44My00NC41LTEwNi41UzQ3My4zMyA1NTkgNDMyIDU1OWMtNDIgMC03Ny42NyAxNC44My0xMDcgNDQuNXMtNDQgNjUuMTctNDQgMTA2LjVjMCA0MiAxNC42NyA3Ny42NyA0NCAxMDdzNjUgNDQgMTA3IDQ0em0wLTU1OWM0MS4zMyAwIDc2LjgzLTE0LjgzIDEwNi41LTQ0LjVTNTgzIDE5Mi4zMyA1ODMgMTUxYzAtNDItMTQuODMtNzcuNjctNDQuNS0xMDdTNDczLjMzIDAgNDMyIDBjLTQyIDAtNzcuNjcgMTQuNjctMTA3IDQ0cy00NCA2NS00NCAxMDdjMCA0MS4zMyAxNC42NyA3Ni44MyA0NCAxMDYuNVMzOTAgMzAyIDQzMiAzMDJ6bTI3NiAyODJjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjY3IDE0LjY3LTEwNiA0NHMtNDQgNjUtNDQgMTA3YzAgNDEuMzMgMTQuNjcgNzYuODMgNDQgMTA2LjVTNjY2LjY3IDU4NCA3MDggNTg0em0tNTU3IDBjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjgzIDE0LjY3LTEwNi41IDQ0UzAgMzkxIDAgNDMzYzAgNDEuMzMgMTQuODMgNzYuODMgNDQuNSAxMDYuNVMxMDkuNjcgNTg0IDE1MSA1ODR6IiBmaWxsPSIjZmZmIi8%2BPC9zdmc%2B&label=tests)](https://aldaviva.testspace.com/spaces/285777) [![Coveralls](https://img.shields.io/coveralls/github/Aldaviva/Unfucked?logo=coveralls&label=test%20coverage)](https://coveralls.io/github/Aldaviva/Unfucked?branch=master)

*Fix egregiously missing or broken functionality in [`HttpClient`](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient). Inspired by [JAX-RS](https://projects.eclipse.org/projects/ee4j.rest) and [Jersey Client](https://eclipse-ee4j.github.io/jersey.github.io/documentation/latest/client.html).*

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3,4" -->

- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [All Unfucked libraries](#all-unfucked-libraries)

<!-- /MarkdownTOC -->

[![Unfuck your house](https://raw.githubusercontent.com/Aldaviva/Unfucked/master/.github/images/frame.jpg)](https://loadingreadyrun.com/videos/view/2484/Crapshots-Ep270-The-Home-Show)

## Installation
```sh
dotnet package add Unfucked.HTTP
```
```cs
using Unfucked;
using Unfucked.HTTP;
```

## Configuration

- Drop-in subclasses of `HttpClient` and `HttpMessageHandler`, and extension methods
    ```cs
    HttpClient http = new UnfuckedHttpClient(); // set the subclass
    http = new UnfuckedHttpClient(new SocketsHttpHandler(){ AllowAutoRedirect = false }); // custom handlers are wrapped automatically
    ```

## Usage

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
        public async ValueTask<HttpRequestMessage> Filter(HttpRequestMessage request, CancellationToken cancellationToken) {
            Console.WriteLine(">> {0} {1}", request.Method, request.RequestUri);
            return request;
        }

        public async ValueTask<HttpResponseMessage> Filter(HttpResponseMessage response, CancellationToken cancellationToken) {
            Console.WriteLine("<< {0} {1}", (int) response.StatusCode, response.RequestMessage?.RequestUri);
            return response;
        }
    }
    ```
- Wire logging (requires .NET &ge; 8 and HTTP 1.1)
    ```cs
    NLog.Logger wireLogger = NLog.LogManager.GetLogger("wire"); // you can plug in any logging implementation, such as NLog or Microsoft.Extensions.Logging
    HttpClient http = new UnfuckedHttpClient();
    http.Register(new WireLoggingFilter(new WireLoggingFilter.Config {
        LogRequestTransmitted = (message, id) => wireLogger.Trace("{0} >> {1}", id, message),
        LogResponseReceived   = (message, id) => wireLogger.Trace("{0} << {1}", id, message),
        IsLogEnabled          = () => wireLogger.IsTraceEnabled
    }));
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
    // automatically mapped from JSON or XML depending on response content type and generic type, falling back to content sniffing response prefix without buffering the entire response into memory
    MyClass response = await http.Target(url).Get<MyClass>();
    ```
    - `HttpResponseMessage` (or just don't pass a generic type parameter to the request method, like `Get()` or `Post()`)
    - `Stream`
    - `ReadOnlyMemory<byte>`
    - `Memory<byte>`
    - `byte[]`
    - `string`
    - JSON (`System.Text.Json`)
        - `JsonDocument`
        - `JsonNode`
        - `JsonObject`
        - `JsonArray`
        - `JsonValue`
        - Any type that can be deserialized from JSON
    - XML (`System.Xml`)
        - `XmlDocument`
        - `XDocument`
        - `XPathNavigator`
        - Any type that can be deserialized from XML
    - HTML ([AngleSharp](https://www.nuget.org/packages/AngleSharp/), provided dependency, so must be declared explicitly in library consumer)
        - `IDocument`
    - `IUtf8SpanParsable`
    - `IParsable`
    - Any type with a public constructor that takes a single `string` parameter
    - Any type `T` with a public static `T Parse(string)` method
    - Any type `T` with a public static `bool TryParse(string, out T?)` method
- Automatic request serialization from types to XML or JSON, the main benefit of which is to consistently apply serializer options, instead of always forgetting to expose and pass around a shared `JsonSerializerOptions` instance
    ```cs
    await http.Target(url).Post(Entity.json(body));
    ```
- Disable chunked transfer encoding if the server is incompatible
    ```cs
    using var http = new UnfuckedHttpClient {
        DefaultRequestHeaders = { TransferEncodingChunked = false }
    };
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

## All Unfucked libraries
- [Unfucked](https://github.com/Aldaviva/Unfucked/tree/master/Unfucked)
- [Unfucked.Caching](https://github.com/Aldaviva/Unfucked/tree/master/Caching)
- [Unfucked.Compression](https://github.com/Aldaviva/Unfucked/tree/master/Compression)
- [Unfucked.DateTime](https://github.com/Aldaviva/Unfucked/tree/master/DateTime)
- [Unfucked.DI](https://github.com/Aldaviva/Unfucked/tree/master/DI)
- **Unfucked.HTTP**
- [Unfucked.ICS](https://github.com/Aldaviva/Unfucked/tree/master/ICS)
- [Unfucked.OBS](https://github.com/Aldaviva/Unfucked/tree/master/OBS)
- [Unfucked.PGP](https://github.com/Aldaviva/Unfucked/tree/master/PGP)
- [Unfucked.STUN](https://github.com/Aldaviva/Unfucked/tree/master/STUN)
- [Unfucked.Twitch](https://github.com/Aldaviva/Unfucked/tree/master/Twitch)
- [Unfucked.Windows](https://github.com/Aldaviva/Unfucked/tree/master/Windows)