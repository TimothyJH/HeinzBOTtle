using System.Text.Json;

namespace HeinzBOTtle.Requirements {

    abstract class Requirement {

        public abstract string Title { get; }
        public abstract string GameTitle { get; }

        public abstract bool MeetsRequirement(Json json);

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

        override public bool MeetsRequirement(Json json) {
            double value = json.GetDouble(Node) ?? 0.0;
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

        override public bool MeetsRequirement(Json json) {
            double value1 = json.GetDouble(Node1) ?? 0.0;
            double value2 = json.GetDouble(Node2) ?? 0.0;
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

        override public bool MeetsRequirement(Json json) {
            double value1 = json.GetDouble(Node1) ?? 0.0;
            double value2 = json.GetDouble(Node2) ?? 0.0;
            return value1 + value2 >= Min;
        }

    }

}
