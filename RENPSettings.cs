using System;
using System.Drawing;
using System.Windows.Forms;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using Newtonsoft.Json;

namespace RENP;

public class RENPSettings : ISettings
{
    public DataSourceSettings DataSourceSettings { get; set; } = new();
    public DebugSettings DebugSettings { get; set; } = new();
    public StashValueSettings StashValueSettings { get; set; } = new();
    public InventoryValueSettings InventoryValueSettings { get; set; } = new();
    public GroundItemSettings GroundItemSettings { get; set; } = new();
    public UniqueIdentificationSettings UniqueIdentificationSettings { get; set; } = new();
    public TradeWindowSettings TradeWindowSettings { get; set; } = new();
    public HoveredItemSettings HoveredItemSettings { get; set; } = new();
    public PriceOverlaySettings PriceOverlaySettings { get; set; } = new();
    public LeagueSpecificSettings LeagueSpecificSettings { get; set; } = new();
    public VisualPriceSettings VisualPriceSettings { get; set; } = new();
    public ToggleNode Enable { get; set; } = new(true);
    public TextNode League { get; set; } = new TextNode("Standard");
    public ToggleNode AutoUpdateLeague { get; set; } = new ToggleNode(true);
    public ToggleNode ShowOverlay { get; set; } = new ToggleNode(true);
    public RangeNode<int> UpdateInterval { get; set; } = new RangeNode<int>(60, 10, 1440);
    public RangeNode<int> ItemsToShow { get; set; } = new RangeNode<int>(10, 5, 30);
    public RangeNode<int> BackgroundAlpha { get; set; } = new RangeNode<int>(200, 0, 255);
    public ColorNode BackgroundColor { get; set; } = new ColorNode(Color.Black);
    public ColorNode TextColor { get; set; } = new ColorNode(Color.White);
    public ColorNode HighValueColor { get; set; } = new ColorNode(Color.Green);
    public RangeNode<int> FontSize { get; set; } = new RangeNode<int>(16, 10, 24);
    public HotkeyNode ToggleOverlayKey { get; set; } = new HotkeyNode(Keys.F9);
    public HotkeyNode ForceUpdateKey { get; set; } = new HotkeyNode(Keys.F10);
    public RangeNode<int> HighValueThreshold { get; set; } = new RangeNode<int>(50, 1, 1000);
    public ToggleNode ShowCurrency { get; set; } = new ToggleNode(true);
    public ToggleNode ShowFragments { get; set; } = new ToggleNode(true);
    public ToggleNode ShowRunes { get; set; } = new ToggleNode(true);
    public ToggleNode ShowUniqueItems { get; set; } = new ToggleNode(true);
}

[Submenu(CollapsedByDefault = true)]
public class DebugSettings
{
    public ToggleNode EnableDebugLogging { get; set; } = new(false);
    public HotkeyNode InspectHoverHotkey { get; set; } = new(Keys.None);

    [JsonIgnore]
    public ButtonNode ResetInspectedItem { get; set; } = new();
}

[Submenu(CollapsedByDefault = true)]
public class DataSourceSettings
{
    public DateTime LastUpdateTime { get; set; } = DateTime.Now;

    public RangeNode<int> ItemUpdatePeriodMs { get; set; } = new(250, 1, 2000);

    public ListNode League { get; set; } = new();

    public ToggleNode SyncCurrentLeague { get; set; } = new(true);

    [JsonIgnore]
    public ButtonNode ReloadPrices { get; set; } = new();

    [JsonProperty("AutoReload_v2")]
    public ToggleNode AutoReload { get; set; } = new(true);

    [Menu(null, "Minutes")]
    public RangeNode<int> ReloadPeriod { get; set; } = new(15, 1, 60);
}

[Submenu(CollapsedByDefault = true)]
public class LeagueSpecificSettings
{
    public ToggleNode ShowRitualWindowPrices { get; set; } = new(true);
    public ToggleNode ShowPurchaseWindowPrices { get; set; } = new(true);

    public ToggleNode ShowExpeditionVendorOverlay { get; set; } = new(false);

    [Menu("Artifact Chaos Prices", "Display chaos equivalent price for items with artifact costs", 7)]
    public ToggleNode ShowArtifactChaosPrices { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = true)]
public class InventoryValueSettings
{
    [Menu(null, "Calculate value for the inventory")]
    public ToggleNode Show { get; set; } = new(true);

    [Menu(null, "Horizontal position of where the value should be drawn")]
    public RangeNode<int> PositionX { get; set; } = new(100, 0, 5000);

    [Menu(null, "Vertical position of where the value should be drawn")]
    public RangeNode<int> PositionY { get; set; } = new(800, 0, 5000);
}

[Submenu(CollapsedByDefault = true)]
public class TradeWindowSettings
{
    public ToggleNode Show { get; set; } = new(true);
    public RangeNode<int> OffsetX { get; set; } = new(0, -2000, 2000);
    public RangeNode<int> OffsetY { get; set; } = new(0, -2000, 2000);
}

[Submenu(CollapsedByDefault = true)]
public class HoveredItemSettings
{
    public ToggleNode Show { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = true)]
public class GroundItemSettings
{
    public ToggleNode PriceHeistRewards { get; set; } = new(true);
    public ToggleNode PriceItemsOnGround { get; set; } = new(true);
    public ToggleNode OnlyPriceUniquesOnGround { get; set; } = new(false);
    public RangeNode<float> GroundPriceTextScale { get; set; } = new(2, 0, 10);
    public ColorNode GroundPriceBackgroundColor { get; set; } = new(Color.Black);
}

[Submenu(CollapsedByDefault = true)]
public class UniqueIdentificationSettings
{
    [JsonIgnore]
    public ButtonNode RebuildUniqueItemArtMappingBackup { get; set; } = new();

    [Menu(null, "Use if you want to ignore what's in game memory and rely only on your custom/builtin file")]
    public ToggleNode IgnoreGameUniqueArtMapping { get; set; } = new(false);

    public ToggleNode ShowRealUniqueNameOnGround { get; set; } = new(true);
    public ToggleNode OnlyShowRealUniqueNameForValuableUniques { get; set; } = new(false);
    public ToggleNode ShowWarningTextForUnknownUniques { get; set; } = new(true);
    public RangeNode<float> UniqueLabelSize { get; set; } = new(0.8f, 0.1f, 1);
    public ColorNode UniqueItemNameTextColor { get; set; } = new(Color.Black);
    public ColorNode UniqueItemNameBackgroundColor { get; set; } = new(Color.FromArgb(175, 96, 37));
    public ColorNode ValuableUniqueItemNameTextColor { get; set; } = new(Color.FromArgb(175, 96, 37));
    public ColorNode ValuableUniqueItemNameBackgroundColor { get; set; } = new(Color.White);
}

[Submenu(CollapsedByDefault = true)]
public class StashValueSettings
{
    [Menu(null, "Calculate value for the current visible stash tab")]
    public ToggleNode Show { get; set; } = new(true);

    [Menu(null, "Horizontal position of where the value should be drawn")]
    public RangeNode<int> PositionX { get; set; } = new(100, 0, 5000);

    [Menu(null, "Vertical position of where the value should be drawn")]
    public RangeNode<int> PositionY { get; set; } = new(100, 0, 5000);

    public RangeNode<int> TopValuedItemCount { get; set; } = new(3, 0, 10);
    public ToggleNode EnableBackground { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = true)]
public class PriceOverlaySettings
{
    public ToggleNode Show { get; set; } = new(true);

    [JsonProperty("DoNotDrawWhileAnItemIsHovered2")]
    public ToggleNode DoNotDrawWhileAnItemIsHovered { get; set; } = new(false);

    public RangeNode<int> BoxHeight { get; set; } = new(15, 0, 100);
}

[Submenu(CollapsedByDefault = true)]
public class VisualPriceSettings
{
    public RangeNode<int> SignificantDigits { get; set; } = new(2, 0, 2);
    public ColorNode FontColor { get; set; } = Color.FromArgb(216, 216, 216);
    public ColorNode BackgroundColor { get; set; } = Color.FromArgb(0, 0, 0);
    public RangeNode<int> ValuableColorThreshold { get; set; } = new(50, 0, 100000);
    public ColorNode ValuableColor { get; set; } = new(Color.Violet);

    [Menu(null, "Set to 0 to disable")]
    public RangeNode<float> MaximalValueForFractionalDisplay { get; set; } = new(0.2f, 0, 1);
}