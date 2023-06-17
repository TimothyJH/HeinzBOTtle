using System.Text.Json;

namespace HeinzBOTtle.Requirements
{

    abstract class Requirement
    {

        public abstract string Title { get; }
        public abstract string GameTitle { get; }

        public abstract bool MeetsRequirement(string json);

        public static List<Requirement> RequirementList = ReqMethods.GenerateRequirementList();

    }

    class SimpleRequirement : Requirement
    {

        override public string Title { get; }
        override public string GameTitle { get; }
        public string Node { get; }
        public int Min { get; }

        public SimpleRequirement(string title, string gameTitle, string node, int min) {
            Title = title;
            GameTitle = gameTitle;
            Node = node;
            Min = min;
        }

        override public bool MeetsRequirement(string json) {
            JsonElement element = JsonMethods.GetNodeValue(json, Node);
            if (element.ValueKind == JsonValueKind.Undefined)
                return false;
            int value = element.GetInt32();
            return value >= Min;
        }

    }

    class CompoundRequirement : Requirement
    {

        override public string Title { get; }
        override public string GameTitle { get; }
        public string Node1 { get; }
        public int Min1 { get; }
        public string Node2 { get; }
        public int Min2 { get; }

        public CompoundRequirement(string title, string gameTitle, string node1, int min1, string node2, int min2) {
            Title = title;
            GameTitle = gameTitle;
            Node1 = node1;
            Min1 = min1;
            Node2 = node2;
            Min2 = min2;
        }

        override public bool MeetsRequirement(string json) {
            JsonElement element1 = JsonMethods.GetNodeValue(json, Node1);
            if (element1.ValueKind == JsonValueKind.Undefined)
                return false;
            JsonElement element2 = JsonMethods.GetNodeValue(json, Node2);
            if (element2.ValueKind == JsonValueKind.Undefined)
                return false;
            int value1 = element1.GetInt32();
            int value2 = element2.GetInt32();
            return value1 >= Min1 && value2 >= Min2;
        }

    }

}
