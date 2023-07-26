namespace HeinzBOTtle.Requirements {

    static class ReqMethods {

        public static List<Requirement> GetRequirementsMet(Json playerJson) {
            List<Requirement> met = new List<Requirement>();
            foreach (Requirement req in HBData.RequirementList) {
                if (req.MeetsRequirement(playerJson))
                    met.Add(req);
            }
            return met;
        }

        public static string FormatRequirementsList(List<Requirement> list) {
            if (list.Count == 0)
                return "";
            string formatted = "";
            foreach (Requirement req in list)
                formatted += req.GameTitle + ", ";
            return formatted[..^2];
        }

    }

}
