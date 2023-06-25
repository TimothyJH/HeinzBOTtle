using System.Text.Json;

namespace HeinzBOTtle {

    class JsonMethods {

        public static Dictionary<string, JsonElement> DeserializeJsonObject(string json) {
            Dictionary<string, JsonElement>? d;
            try {
                d = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            } catch {
                d = null;
            }
            if (d == null)
                return new Dictionary<string, JsonElement>();
            return d;
        }

        public static JsonElement GetNodeValue(string json, string node) {
            if (node.Contains('.')) {
                string[] nodes = node.Split('.', 2);
                Dictionary<string, JsonElement> outerMap = DeserializeJsonObject(json);
                if (outerMap.Count == 0)
                    return new JsonElement();
                JsonElement target;
                try {
                    target = GetNodeValue(outerMap[nodes[0]].GetRawText(), nodes[1]);
                } catch {
                    target = new JsonElement();
                }
                return target;
            }
            Dictionary<string, JsonElement> map = DeserializeJsonObject(json);
            return map[node];
        }

        

    }

}
