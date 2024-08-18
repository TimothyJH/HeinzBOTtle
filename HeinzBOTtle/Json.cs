using System.Text.Json;

namespace HeinzBOTtle;

/// <summary>
/// Represents a JSON object that is only recursively parsed when necessary. Supports access by dot-delimited nodes.
/// </summary>
public class Json {

    /// <summary>The top-level representation of the JSON object.</summary>
    private Dictionary<string, object>? data;

    public Json(string json) {
        try {
            data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        } catch {
            data = new Dictionary<string, object>();
        }
    }

    public Json(JsonElement json) {
        if (json.ValueKind != JsonValueKind.Object)
            data = new Dictionary<string, object>();
        else {
            try {
                data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            } catch {
                data = new Dictionary<string, object>();
            }
        }
    }

    /// <returns>True if the JSON object has no entries, otherwise false.</returns>
    public bool IsEmpty() {
        return data == null || data.Count == 0;
    }

    /// <param name="node">The dot-delimited JSON node holding the value</param>
    /// <returns>The data type found at the provided dot-delimited node.</returns>
    public JsonValueKind GetValueKind(string node) {
        if (data == null)
            return JsonValueKind.Undefined;
        Dictionary<string, object>? position = data;
        while (node.Contains('.')) {
            string[] nodes = node.Split('.', 2);
            if ((nodes.Length < 2) || !position.ContainsKey(nodes[0]))
                return JsonValueKind.Undefined;
            object? value = position[nodes[0]];
            if (value == null)
                return JsonValueKind.Undefined;
            else if (value.GetType() == typeof(Dictionary<string, object>)) {
                position = (Dictionary<string, object>)value;
                node = nodes[1];
                continue;
            } else if (value.GetType() == typeof(JsonElement)) {
                JsonElement element = (JsonElement)value;
                if (element.ValueKind != JsonValueKind.Object)
                    return JsonValueKind.Undefined;
                try {
                    value = JsonSerializer.Deserialize<Dictionary<string, object>>(element);
                } catch {
                    value = null;
                }
                if (value == null)
                    return JsonValueKind.Undefined;
                position[nodes[0]] = value;
                position = (Dictionary<string, object>)position[nodes[0]];
                node = nodes[1];
                continue;
                } else
                return JsonValueKind.Undefined;
        }
        if (!position.ContainsKey(node))
            return JsonValueKind.Undefined;
        object target = position[node];
        if (target == null)
            return JsonValueKind.Undefined;
        else if (target.GetType() == typeof(JsonElement))
            return ((JsonElement)target).ValueKind;
        else if (target.GetType() == typeof(Dictionary<string, object>)) {
            return JsonValueKind.Object;
        } else
            return JsonValueKind.Undefined;
    }

    /// <param name="node">The dot-delimited JSON node holding the value</param>
    /// <returns>The data type found at the provided dot-delimited node.</returns>
    public JsonElement GetElement(string node) {
        if (data == null)
            return new JsonElement();
        Dictionary<string, object>? position = data;
        while (node.Contains('.')) {
            string[] nodes = node.Split('.', 2);
            if ((nodes.Length < 2) || !position.ContainsKey(nodes[0]))
                return new JsonElement();
            object? value = position[nodes[0]];
            if (value == null)
                return new JsonElement();
            else if (value.GetType() == typeof(Dictionary<string, object>)) {
                position = (Dictionary<string, object>)value;
                node = nodes[1];
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
                node = nodes[1];
                continue;
            } else
                return new JsonElement();
        }
        if (!position.ContainsKey(node))
            return new JsonElement();
        object target = position[node];
        if (target == null)
            return new JsonElement();
        else if (target.GetType() == typeof(JsonElement))
            return (JsonElement)target;
        else if (target.GetType() == typeof(Dictionary<string, object>)) {
            return new JsonElement(); // Maybe turn this back into a JsonElement holding an object somehow?
        } else
            return new JsonElement();
    }

    /// <param name="node">The dot-delimited JSON node holding the value</param>
    /// <returns>The string found at the provided dot-delimited node if it exists, otherwise null.</returns>
    public string? GetString(string node) {
        JsonElement element = GetElement(node);
        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    /// <param name="node">The dot-delimited JSON node holding the value</param>
    /// <returns>The double found at the provided dot-delimited node if it exists, otherwise null.</returns>
    public double? GetDouble(string node) {
        JsonElement element = GetElement(node);
        return element.ValueKind == JsonValueKind.Number ? element.GetDouble() : null;
    }

    /// <param name="node">The dot-delimited JSON node holding the value</param>
    /// <returns>The unsigned long found at the provided dot-delimited node if it exists, otherwise null.</returns>
    public ulong? GetUInt64(string node) {
        JsonElement element = GetElement(node);
        return element.ValueKind == JsonValueKind.Number ? element.GetUInt64() : null;
    }

    /// <param name="node">The dot-delimited JSON node holding the value</param>
    /// <returns>The boolean found at the provided dot-delimited node if it exists, otherwise null.</returns>
    public bool? GetBoolean(string node) {
        JsonElement element = GetElement(node);
        return element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False ? element.GetBoolean() : null;
    }

    /// <param name="node">The dot-delimited JSON node holding the value</param>
    /// <returns>The array found at the provided dot-delimited node formatted as a list if it exists, otherwise null.</returns>
    public List<JsonElement>? GetArray(string node) {
        JsonElement element = GetElement(node);
        if (element.ValueKind == JsonValueKind.Array) {
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

}
