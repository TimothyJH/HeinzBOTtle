using System.Text.Json;

namespace HeinzBOTtle {
    
    class Json {

        private Dictionary<string, object>? data;

        public Json(string json) {
            try {
                data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            } catch {
                data = new Dictionary<string, object>();
            }
        }

        public bool IsEmpty() {
            return data == null || data.Count == 0;
        }

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

        public string? GetString(string node) {
            JsonElement element = GetElement(node);
            return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        }

        public double? GetDouble(string node) {
            JsonElement element = GetElement(node);
            return element.ValueKind == JsonValueKind.Number ? element.GetDouble() : null;
        }

        public bool? GetBoolean(string node) {
            JsonElement element = GetElement(node);
            return element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False ? element.GetBoolean() : null;
        }

    }
}
