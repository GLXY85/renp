using System.Collections.Generic;

namespace RENP.API.Poe2Scout.Models;

public class NinjaLeagueListRootObject
{
    public List<NinjaLeague> economyLeagues { get; set; }
}

public class NinjaLeague
{
    public string name { get; set; }
    public bool indexed { get; set; }
} 