using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using System.Windows.Forms;

namespace NinjaPricer
{
    public class NinjaPricerSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);
        public ToggleNode Debug { get; set; } = new ToggleNode(false);
        
        // Настройки для работы с API
        public TextNode League { get; set; } = new TextNode("Standard");
        public ToggleNode AutoUpdateLeague { get; set; } = new ToggleNode(true);
        public ToggleNode ShowOverlay { get; set; } = new ToggleNode(true);
        public RangeNode<int> UpdateInterval { get; set; } = new RangeNode<int>(60, 10, 1440);
        public RangeNode<int> ItemsToShow { get; set; } = new RangeNode<int>(10, 5, 30);
        public RangeNode<int> BackgroundAlpha { get; set; } = new RangeNode<int>(200, 0, 255);
        
        // Визуальные настройки
        public ColorNode BackgroundColor { get; set; } = new ColorNode(Color.Black);
        public ColorNode TextColor { get; set; } = new ColorNode(Color.White);
        public ColorNode HighValueColor { get; set; } = new ColorNode(Color.Green);
        public RangeNode<int> FontSize { get; set; } = new RangeNode<int>(16, 10, 24);
        
        // Горячие клавиши
        public HotkeyNode ToggleOverlayKey { get; set; } = new HotkeyNode(Keys.F9);
        public HotkeyNode ForceUpdateKey { get; set; } = new HotkeyNode(Keys.F10);
        
        // Пороговые значения для подсветки предметов
        public RangeNode<int> HighValueThreshold { get; set; } = new RangeNode<int>(50, 1, 1000);
        
        // Фильтрация категорий
        public ToggleNode ShowCurrency { get; set; } = new ToggleNode(true);
        public ToggleNode ShowFragments { get; set; } = new ToggleNode(true);
        public ToggleNode ShowRunes { get; set; } = new ToggleNode(true);
        public ToggleNode ShowUniqueItems { get; set; } = new ToggleNode(true);
        
        public NinjaPricerSettings()
        {
            Enable.OnValueChanged += (sender, e) => 
            {
                ToggleNodeChanged(sender, e);
            };
        }
        
        private void ToggleNodeChanged<T>(T sender, T e) {}
    }
} 