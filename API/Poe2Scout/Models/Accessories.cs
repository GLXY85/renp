using System;

namespace NinjaPricer.API.Poe2Scout.Models;

public class Accessories
{
    public class RootObject : IPaged<Item>
    {
        public Item[] items { get; set; }
        public int total { get; set; }
        public int pages { get; set; }
        public int current_page { get; set; }
    }

    public class Item
    {
        public string id { get; set; }
        public string category { get; set; }
        public string type { get; set; }
        public string text { get; set; }
        public string name { get; set; }
        public bool unique { get; set; }
        public string icon { get; set; }
        public LatestPrice latest_price { get; set; }
        public PriceHistory[] price_history { get; set; }
        public ItemMetadata item_metadata { get; set; }
        public string localisation_name { get; set; }
    }

    public class LatestPrice
    {
        public int id { get; set; }
        public string item_id { get; set; }
        public float price { get; set; }
        public CurrencyId currency_id { get; set; }
        public int quantity { get; set; }
        public DateTime created_at { get; set; }
        public float nominal_price { get; set; }
        public bool bid { get; set; }
        public bool flag { get; set; }
    }

    public class CurrencyId
    {
        public string id { get; set; }
        public string icon { get; set; }
        public string currency_type { get; set; }
        public string localisation_name { get; set; }
    }

    public class ItemMetadata
    {
        public string name { get; set; }
        public string base_type { get; set; }
        public int item_level { get; set; }
        public string icon { get; set; }
        public Properties properties { get; set; }
        public string[] implicit_mods { get; set; }
        public string[] explicit_mods { get; set; }
        public string flavor_text { get; set; }
        public Requirements requirements { get; set; }
        public object description { get; set; }
    }

    public class Properties
    {
        public object Ring { get; set; }
        public object Amulet { get; set; }
        public object Belt { get; set; }
        public string CharmSlots { get; set; }
    }

    public class Requirements
    {
        public string Level { get; set; }
    }

    public class PriceHistory
    {
        public float price { get; set; }
        public float nominal_price { get; set; }
        public DateTime time { get; set; }
    }
}