using System.Collections.Generic;
using NinjaPricer.API.Poe2Scout.Models;

namespace NinjaPricer.API.Poe2Scout;

public class CollectiveApiData
{
    public List<Armour.Item> Armour { get; set; }
    public List<Currency.Item> Currency { get; set; }
    public List<Weapons.Item> Weapons { get; set; }
    public List<Breach.Item> Breach { get; set; }
    public List<Accessories.Item> Accessories { get; set; }
    public List<Ultimatum.Item> Ultimatum { get; set; }
    public List<Delirium.Item> Delirium { get; set; }
    public List<Essences.Item> Essences { get; set; }
    public List<Ritual.Item> Ritual { get; set; }
}