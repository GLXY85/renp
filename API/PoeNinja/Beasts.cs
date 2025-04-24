using System.Collections.Generic;

namespace RENP.API.PoeNinja;

public class Beasts
{
    public class RootObject
    {
        public List<Line> lines { get; set; }
    }

    public class Line
    {
        public int id { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public string baseType { get; set; }
        public int? itemClass { get; set; }
        public Sparkline sparkline { get; set; }
        public Lowconfidencesparkline lowConfidenceSparkline { get; set; }
        public object[] implicitModifiers { get; set; }
        public object[] explicitModifiers { get; set; }
        public string flavourText { get; set; }
        public float? chaosValue { get; set; }
        public float? exaltedValue { get; set; }
        public float? divineValue { get; set; }
        public int? count { get; set; }
        public string detailsId { get; set; }
        public object[] tradeInfo { get; set; }
        public int? listingCount { get; set; }
    }

    public class Sparkline
    {
        public float?[] data { get; set; }
        public float? totalChange { get; set; }
    }

    public class Lowconfidencesparkline
    {
        public float?[] data { get; set; }
        public float? totalChange { get; set; }
    }
}