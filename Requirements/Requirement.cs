using System.Text.Json;

namespace HeinzBOTtle.Requirements {

    abstract class Requirement {

        public abstract string Title { get; }
        public abstract string GameTitle { get; }

        public abstract bool MeetsRequirement(string json);

        public static List<Requirement> RequirementList = ReqMethods.GenerateRequirementList();

    }

    class SimpleRequirement : Requirement {

        override public string Title { get; }
        override public string GameTitle { get; }
        public string Node { get; }
        public double Min { get; }

        public SimpleRequirement(string title, string gameTitle, string node, double min) {
            Title = title;
            GameTitle = gameTitle;
            Node = node;
            Min = min;
        }

        override public bool MeetsRequirement(string json) {
            JsonElement element = JsonMethods.GetNodeValue(json, Node);
            if (element.ValueKind == JsonValueKind.Undefined)
                return false;
            double value = element.GetDouble();
            return value >= Min;
        }

    }

    class CompoundRequirement : Requirement {

        override public string Title { get; }
        override public string GameTitle { get; }
        public string Node1 { get; }
        public double Min1 { get; }
        public string Node2 { get; }
        public double Min2 { get; }

        public CompoundRequirement(string title, string gameTitle, string node1, double min1, string node2, double min2) {
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
            double value1 = element1.GetDouble();
            double value2 = element2.GetDouble();
            return value1 >= Min1 && value2 >= Min2;
        }

    }

    class AdditiveRequirement : Requirement {

        override public string Title { get; }
        override public string GameTitle { get; }
        public string Node1 { get; }
        public string Node2 { get; }
        public double Min { get; }

        public AdditiveRequirement(string title, string gameTitle, string node1, string node2, double min) {
            Title = title;
            GameTitle = gameTitle;
            Node1 = node1;
            Node2 = node2;
            Min = min;
        }

        override public bool MeetsRequirement(string json) {
            JsonElement element1 = JsonMethods.GetNodeValue(json, Node1);
            double value1;
            if (element1.ValueKind == JsonValueKind.Undefined)
                value1 = 0.0;
            else
                value1 = element1.GetDouble();
            JsonElement element2 = JsonMethods.GetNodeValue(json, Node2);
            double value2;
            if (element2.ValueKind == JsonValueKind.Undefined)
                value2 = 0.0;
            else
                value2 = element2.GetDouble();
            return value1 + value2 >= Min;
        }

    }

}
