using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;

namespace Unfucked.Serialization.Json;

[Obsolete("doesn't work when two properties have nested property chains that start with the same ancestor")]
public static class JsonSerializerExtensions {

    /// <exception cref="ArgumentException"></exception>
    public static JsonSerializerOptions WithNestedPropertyNames(this JsonSerializerOptions options) {
        options.TypeInfoResolver = options.TypeInfoResolver switch {
            null                          => new DefaultJsonTypeInfoResolver { Modifiers = { Modifier } },
            DefaultJsonTypeInfoResolver d => d.WithAddedModifier(Modifier),
            _                             => throw new ArgumentException("JsonSerializerOptions TypeInfoResolver is not a DefaultJsonTypeInfoResolver", nameof(options))
        };
        return options;
    }

    internal static void Modifier(JsonTypeInfo typeInfo) {
        foreach (JsonPropertyInfo propertyInfo in typeInfo.Properties) {
            if (propertyInfo.Name is var name && name.Split('.') is { Length: > 1 } nameSegments && propertyInfo.Set is { } originalSet) {
                // propertyInfo.Name = nameSegments[0];
                JsonConverter leafConverter = propertyInfo.CustomConverter ?? propertyInfo.Options.GetConverter(propertyInfo.PropertyType);
                propertyInfo.CustomConverter = new NestedPropertyConverter(nameSegments.Skip(1), propertyInfo.PropertyType, leafConverter);
            }
        }
    }

    internal class NestedPropertyConverter(IEnumerable<string> nameChain, Type leafType, JsonConverter leafConverter): JsonConverter<object?> {

        public static readonly Lazy<JsonConverter<JsonNode?>> JsonNodeConverter = new(() =>
            (JsonConverter<JsonNode?>) Type.GetType("System.Text.Json.Serialization.Converters.JsonNodeConverter, System.Text.Json, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51")!
                .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)!.GetValue(null), LazyThreadSafetyMode.PublicationOnly);

        private delegate object? ReadDelegate(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

        /// <exception cref="JsonException"></exception>
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            Utf8JsonReader readerClone = reader;
            foreach (string childName in nameChain) {
                if (readerClone.TokenType != JsonTokenType.StartObject) {
                    throw new JsonException($"Expected {{ in nested JSON, got {readerClone.TokenType}");
                }

                readerClone.Read();
                while (readerClone.TokenType != JsonTokenType.EndObject) {
                    if (readerClone.TokenType != JsonTokenType.PropertyName) {
                        throw new JsonException($"Expected property name in nested JSON, got {readerClone.TokenType}");
                    }

                    string? propertyName = readerClone.GetString();
                    if (propertyName == childName) {
                        readerClone.Read();
                        break;
                    } else {
                        readerClone.Skip();
                        readerClone.Read();
                    }
                }
            }

            MethodInfo readMethod = typeof(JsonConverter<>).MakeGenericType([leafConverter.Type])
                .GetMethod("Read", [typeof(Utf8JsonReader).MakeByRefType(), typeof(Type), typeof(JsonSerializerOptions)])!;
            ReadDelegate readDelegate  = (ReadDelegate) readMethod.CreateDelegate(typeof(ReadDelegate), leafConverter);
            object?      convertedLeaf = readDelegate(ref readerClone, typeToConvert, options);

            reader.Skip();

            return convertedLeaf;
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options) {
            throw new NotImplementedException();
        }

    }

}