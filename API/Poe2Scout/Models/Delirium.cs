using System;

namespace NinjaPricer.API.Poe2Scout.Models;

public class Delirium
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
        public bool unique { get; set; }
        public string icon { get; set; }
        public LatestPrice latest_price { get; set; }
        public string currency_type { get; set; }
        public string exchange_id { get; set; }
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
        public string icon { get; set; }
        public int stack_size { get; set; }
        public int max_stack_size { get; set; }
        public string description { get; set; }
        public string[] effect { get; set; }
    }

    public class PriceHistory
    {
        public float price { get; set; }
        public float nominal_price { get; set; }
        public DateTime time { get; set; }
    }
}