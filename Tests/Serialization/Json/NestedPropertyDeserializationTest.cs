using System.Text.Json;
using System.Text.Json.Serialization;
using Unfucked.Serialization.Json;

namespace Tests.Serialization.Json;

public class NestedPropertyDeserializationTest {

    [Fact(Skip = "obsolete")]
    public void TwoLevels() {
        const string          json        = """{ "a": { "e": { "f": "g" }, "b": { "c": "d" } }, "foo": "bar" }""";
        JsonSerializerOptions jsonOptions = new JsonSerializerOptions().WithNestedPropertyNames();

        MyClass actual = JsonSerializer.Deserialize<MyClass>(json, jsonOptions)!;
        actual.c.Should().Be("d");
        actual.f.Should().Be("g");
        actual.foo.Should().Be("bar");
    }

    public class MyClass {

        [JsonPropertyName("a.b.c")]
        public string c { get; set; }

        [JsonPropertyName("a.e.f")]
        public string f { get; set; }

        // [JsonConverter(typeof(StubConverter))]
        public string? foo { get; set; }

    }

}