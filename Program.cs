using HeinzBOTtle.Requirements;

// This is a REALLY old file just used for the base commit lol.

class Program {

    static HttpClient hypertexter = new HttpClient();
    static string key = "";

    static Task Main(string[] args) => new Program().MainAsync();

    async Task MainAsync() {
        string name = "";
        var response = await hypertexter.GetAsync("https://api.hypixel.net/player?key=" + key + "&name=" + name);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync();
        var met = ReqMethods.GetRequirementsMet(json);
        Console.WriteLine(json);
        Console.WriteLine();
        Console.WriteLine(name);
        Console.WriteLine("=============");
        foreach (Requirement req in met) {
            Console.WriteLine(req.Title + " - " + req.GameTitle);
        }
        Console.WriteLine("=============");
    }

}