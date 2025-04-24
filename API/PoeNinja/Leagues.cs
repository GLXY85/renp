namespace RENP.API.PoeNinja;

public class NinjaLeagueListRootObject
{
    public EconomyLeague[] economyLeagues { get; set; }
}

public class EconomyLeague
{
    public string name { get; set; }
    public string url { get; set; }
    public string displayName { get; set; }
    public bool? hardcore { get; set; }
    public bool indexed { get; set; }
}