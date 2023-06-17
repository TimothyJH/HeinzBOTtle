using System.Text.Json;
using System.Xml.Linq;

namespace HeinzBOTtle.Requirements {

    class PitRequirement : Requirement {

        override public string Title { get; }
        override public string GameTitle { get; }

        public PitRequirement() {
            this.Title = "Cookie Clicker";
            this.GameTitle = "The Hypixel Pit";
        }

        override public bool MeetsRequirement(string json) {
            JsonElement element = JsonMethods.GetNodeValue(json, "player.stats.Pit.profile.prestiges");
            if (element.ValueKind == JsonValueKind.Undefined)
                return false;
            return element.GetArrayLength() >= 8;
        }

    }

    class VZRequirement : Requirement {

        override public string Title { get; }
        override public string GameTitle { get; }

        public VZRequirement() {
            this.Title = "Van Helsing";
            this.GameTitle = "VampireZ";
        }

        override public bool MeetsRequirement(string json) {
            JsonElement humanWinsElement = JsonMethods.GetNodeValue(json, "player.stats.VampireZ.human_wins");
            int humanWins;
            if (humanWinsElement.ValueKind == JsonValueKind.Undefined)
                humanWins = 0;
            else
                humanWins = humanWinsElement.GetInt32();
            JsonElement vampireWinsElement = JsonMethods.GetNodeValue(json, "player.stats.VampireZ.vampire_wins");
            int vampireWins;
            if (vampireWinsElement.ValueKind == JsonValueKind.Undefined)
                vampireWins = 0;
            else
                vampireWins = vampireWinsElement.GetInt32();
            return humanWins + vampireWins >= 150;
        }

    }

}
