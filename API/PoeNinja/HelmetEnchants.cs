using System.Collections.Generic;

namespace RENP.API.PoeNinja;

public class HelmetEnchants
{
    public class Sparkline
    {
        public List<object> data { get; set; }
        public double totalChange { get; set; }
    }

    public class LowConfidenceSparkline
    {
        public List<object> data { get; set; }
        public double totalChange { get; set; }
    }

    public class Line
    {
        public int id { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public int mapTier { get; set; }
        public int levelRequired { get; set; }
        public object baseType { get; set; }
        public int stackSize { get; set; }
        public string variant { get; set; }
        public object prophecyText { get; set; }
        public object artFilename { get; set; }
        public int links { get; set; }
        public int itemClass { get; set; }
        public Sparkline sparkline { get; set; }
        public LowConfidenceSparkline lowConfidenceSparkline { get; set; }
        public List<object> implicitModifiers { get; set; }
        public List<object> explicitModifiers { get; set; }
        public object flavourText { get; set; }
        public bool corrupted { get; set; }
        public int gemLevel { get; set; }
        public int gemQuality { get; set; }
        public string itemType { get; set; }
        public double chaosValue { get; set; }
        public double divinePrice { get; set; }
        public int count { get; set; }
    }

    public class RootObject
    {
        public List<Line> lines { get; set; }
    }
}