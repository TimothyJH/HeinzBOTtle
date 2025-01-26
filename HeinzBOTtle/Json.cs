using System.Text.Json;

namespace HeinzBOTtle;

/// <summary>
/// Represents a JSON object that is only recursively parsed when necessary. Supports access by dot-delimited paths to nodes.
/// </summary>
public class Json {

    /// <summary>The top-level representation of the JSON object.</summary>
    private readonly Dictionary<string, object> data;

    public Json(string json) {
        try {
            data = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        } catch {
            data = new Dictionary<string, object>();
        }
    }

    public Json(JsonElement json) {
        if (json.ValueKind != JsonValueKind.Object)
            data = new Dictionary<string, object>();
        else {
            try {
                data = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            } catch {
                data = new Dictionary<string, object>();
            }
        }
    }

    /// <returns>True if the JSON object has no entries, otherwise false.</returns>
    public bool IsEmpty() {
        return data == null || data.Count == 0;
    }

    /// <param name="path">The dot-delimited path to the JSON node holding the value</param>
    /// <returns>The data type found at the provided path.</returns>
    public JsonValueKind GetValueKind(string path) {
        object item = GetElement(path);
        if (item is JsonElement element)
            return element.ValueKind;
        else if (item is Dictionary<string, object>)
            return JsonValueKind.Object;
        else
            return JsonValueKind.Undefined;
    }

    /// <param name="path">The dot-delimited path to the JSON node holding the target value</param>
    /// <returns>The string found at the provided path if it exists, otherwise null.</returns>
    public string? GetString(string path) {
        object item = GetElement(path);
        return item is JsonElement element && element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    /// <param name="path">The dot-delimited path to the JSON node holding the target value</param>
    /// <returns>The double found at the provided path if it exists, otherwise null.</returns>
    public double? GetDouble(string path) {
        object item = GetElement(path);
        return item is JsonElement element && element.ValueKind == JsonValueKind.Number ? element.GetDouble() : null;
    }

    /// <param name="path">The dot-delimited path to the JSON node holding the target value</param>
    /// <returns>The unsigned long found at the provided path if it exists, otherwise null.</returns>
    public ulong? GetUInt64(string path) {
        object item = GetElement(path);
        return item is JsonElement element && element.ValueKind == JsonValueKind.Number ? element.GetUInt64() : null;
    }

    /// <param name="path">The dot-delimited path to the JSON node holding the target value</param>
    /// <returns>The boolean found at the provided path if it exists, otherwise null.</returns>
    public bool? GetBoolean(string path) {
        object item = GetElement(path);
        return item is JsonElement element && (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False) ? element.GetBoolean() : null;
    }

    /// <param name="path">The dot-delimited path to the JSON node holding the target value</param>
    /// <returns>The array found at the provided path formatted as a list if it exists, otherwise null.</returns>
    public List<JsonElement>? GetArray(string path) {
        object item = GetElement(path);
        if (item is JsonElement element && element.ValueKind == JsonValueKind.Array) {
            List<JsonElement>? list;
            try {
                list = JsonSerializer.Deserialize<List<JsonElement>>(element);
            } catch {
                list = null;
            }
            return list;
        } else
            return null;
    }

    /// <param name="path">The dot-delimited path to the JSON node holding the target value</param>
    /// <returns>The object found at the provided path as a Dictionary with key and value types as specified if it exists, otherwise null.</returns>
    public Dictionary<K, V>? GetObject<K, V>(string path) where K : notnull {
        object item = GetElement(path);
        if (item is JsonElement element && element.ValueKind == JsonValueKind.Object) {
            try {
                return JsonSerializer.Deserialize<Dictionary<K, V>>(element);
            } catch {
                return null;
            }
        }
        if (item is Dictionary<string, object> obj) {
            string serialized;
            try {
                serialized = JsonSerializer.Serialize(obj, typeof(Dictionary<string, object>));
            } catch {
                return null;
            }
            try {
                return JsonSerializer.Deserialize<Dictionary<K, V>>(serialized);
            } catch {
                return null;
            }
        }
        return null;
    }

    /// <param name="path">The dot-delimited path to the JSON node holding the target value</param>
    /// <returns>The data found at the provided path, which will be a <see cref="Dictionary{string, object}"/> if an object, otherwise a <see cref="JsonElement"/>.</returns>
    private object GetElement(string path) {
        if (IsEmpty())
            return new JsonElement();
        Dictionary<string, object> position = data;
        while (path.Contains('.')) {
            string[] nodes = path.Split('.', 2);
            if ((nodes.Length < 2) || !position.TryGetValue(nodes[0], out object? value))
                return new JsonElement();
            if (value == null)
                return new JsonElement();
            else if (value.GetType() == typeof(Dictionary<string, object>)) {
                position = (Dictionary<string, object>)value;
                path = nodes[1];
                continue;
            } else if (value.GetType() == typeof(JsonElement)) {
                JsonElement element = (JsonElement)value;
                if (element.ValueKind != JsonValueKind.Object)
                    return new JsonElement();
                try {
                    value = JsonSerializer.Deserialize<Dictionary<string, object>>(element);
                } catch {
                    value = null;
                }
                if (value == null)
                    return new JsonElement();
                position[nodes[0]] = value;
                position = (Dictionary<string, object>)position[nodes[0]];
                path = nodes[1];
                continue;
            } else
                return new JsonElement();
        }
        if (!position.TryGetValue(path, out object? target))
            return new JsonElement();
        if (target == null)
            return new JsonElement();
        else if (target.GetType() == typeof(JsonElement) || target.GetType() == typeof(Dictionary<string, object>))
            return target;
        else
            return new JsonElement();
    }

}
