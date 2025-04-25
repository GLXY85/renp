using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ExileCore2;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.PoEMemory.Models;
using ExileCore2.Shared.Nodes;
using Newtonsoft.Json;
using RENP.API.Poe2Scout;
using RENP.API.Poe2Scout.Models;
using CollectiveApiData = RENP.API.Poe2Scout.CollectiveApiData;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using SharpDX;

namespace RENP;

public partial class RENP : BaseSettingsPlugin<RENPSettings>
{
    private string NinjaDirectory;
    private CollectiveApiData CollectedData => _downloader.CollectedData;
    private const string CustomUniqueArtMappingPath = "uniqueArtMapping.json";
    private const string DefaultUniqueArtMappingPath = "uniqueArtMapping.default.json";
    private int _updating;
    public Dictionary<string, List<string>> UniqueArtMapping = new Dictionary<string, List<string>>();
    private readonly DataDownloader _downloader = new DataDownloader();
    private HttpClient httpClient;
    private Dictionary<string, PriceInfo> priceData = new Dictionary<string, PriceInfo>();
    private DateTime lastUpdate = DateTime.MinValue;
    private bool isUpdating = false;
    private CancellationTokenSource cancellationTokenSource;
    private string currentLeague = "Standard";
    private Dictionary<string, string> categoryMapping;
    private bool overlayVisible = true;
    private Vector2 overlayPosition = new Vector2(100, 100);
    private List<ItemOverlayInfo> topValuedItems = new List<ItemOverlayInfo>();

    // Новый класс для хранения информации о ценах предметов
    private class PriceInfo
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public double ChaosValue { get; set; }
        public string IconUrl { get; set; }
    }

    // Класс для отображения предметов в оверлее
    private class ItemOverlayInfo
    {
        public string Name { get; set; }
        public double ChaosValue { get; set; }
        public string Category { get; set; }
    }

    // Класс для парсинга ответа API лиг
    private class LeagueInfo
    {
        public string Value { get; set; }
        public double DivinePrice { get; set; }
    }

    // Класс для парсинга ответа API категорий
    private class CategoryResponse
    {
        public List<Category> Currency_categories { get; set; }
        public List<Category> Unique_categories { get; set; }
    }

    private class Category
    {
        public int Id { get; set; }
        public string ApiId { get; set; }
        public string Label { get; set; }
        public string Icon { get; set; }
    }

    // Классы для парсинга ответа API предметов
    private class ItemsResponse
    {
        public int CurrentPage { get; set; }
        public int Pages { get; set; }
        public int Total { get; set; }
        public List<ItemData> Items { get; set; }
    }

    private class ItemData
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string CategoryApiId { get; set; }
        public double CurrentPrice { get; set; }
        public List<PriceLogEntry> PriceLogs { get; set; }
    }

    private class PriceLogEntry
    {
        public double Price { get; set; }
        public DateTime Time { get; set; }
        public int Quantity { get; set; }
    }

    public override bool Initialise()
    {
        _downloader.DataDirectory = Path.Join(DirectoryFullName, "poescoutdata");
        _downloader.Settings = Settings;
        _downloader.log = LogMessage;
        NinjaDirectory = Path.Join(DirectoryFullName, "NinjaData");
        Directory.CreateDirectory(NinjaDirectory);

        UpdateLeagueList();
        _downloader.StartDataReload(Settings.DataSourceSettings.League.Value, false);

        Settings.DataSourceSettings.ReloadPrices.OnPressed += () => _downloader.StartDataReload(Settings.DataSourceSettings.League.Value, true);
        Settings.UniqueIdentificationSettings.RebuildUniqueItemArtMappingBackup.OnPressed += () =>
        {
            var mapping = GetGameFileUniqueArtMapping();
            if (mapping != null)
            {
                File.WriteAllText(Path.Join(DirectoryFullName, CustomUniqueArtMappingPath), JsonConvert.SerializeObject(mapping, Formatting.Indented));
            }
        };
        Settings.UniqueIdentificationSettings.IgnoreGameUniqueArtMapping.OnValueChanged += (_, _) =>
        {
            UniqueArtMapping = GetUniqueArtMapping();
        };
        Settings.DataSourceSettings.SyncCurrentLeague.OnValueChanged += (_, _) => SyncCurrentLeague();
        CustomItem.InitCustomItem(this);
        Settings.DebugSettings.ResetInspectedItem.OnPressed += () =>
        {
            _inspectedItem = null;
        };
        GameController.PluginBridge.SaveMethod("RENP.GetValue", (Entity e) =>
        {
            var customItem = new CustomItem(e, null);
            GetValue(customItem);
            return customItem.PriceData.MinChaosValue;
        });
        GameController.PluginBridge.SaveMethod("RENP.GetBaseItemTypeValue", (BaseItemType baseItemType) =>
        {
            var customItem = new CustomItem(baseItemType);
            GetValue(customItem);
            return customItem.PriceData.MinChaosValue;
        });

        // Инициализация HTTP клиента
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("Referer", "https://poe2scout.com/");
        httpClient.DefaultRequestHeaders.Add("Origin", "https://poe2scout.com");
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Инициализация категорий
        categoryMapping = new Dictionary<string, string>
        {
            { "currency", "Валюта" },
            { "fragments", "Фрагменты" },
            { "runes", "Руны" },
            { "talismans", "Талисманы" },
            { "essences", "Эссенции" },
            { "accessory", "Аксессуары" },
            { "armour", "Броня" },
            { "flask", "Флаконы" },
            { "jewel", "Камни" },
            { "map", "Карты" },
            { "weapon", "Оружие" }
        };

        // Первоначальное обновление данных
        if (Settings.AutoUpdateLeague)
        {
            Task.Run(async () => 
            {
                await UpdateActiveLeague();
                await UpdatePriceData();
            });
        }
        else
        {
            currentLeague = Settings.League.Value;
            Task.Run(async () => await UpdatePriceData());
        }

        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        _inspectedItem = null;
        UniqueArtMapping = GetUniqueArtMapping();
        SyncCurrentLeague();

        // Обновляем данные при переходе в новую локацию, если прошло достаточно времени
        if ((DateTime.Now - lastUpdate).TotalMinutes >= Settings.UpdateInterval)
        {
            Task.Run(async () => await UpdatePriceData());
        }
    }

    private void SyncCurrentLeague()
    {
        if (Settings.DataSourceSettings.SyncCurrentLeague)
        {
            var playerLeague = PlayerLeague;
            if (playerLeague != null)
            {
                if (!Settings.DataSourceSettings.League.Values.Contains(playerLeague))
                {
                    Settings.DataSourceSettings.League.Values.Add(playerLeague);
                }

                if (Settings.DataSourceSettings.League.Value != playerLeague)
                {
                    Settings.DataSourceSettings.League.Value = playerLeague;
                    _downloader.StartDataReload(Settings.DataSourceSettings.League.Value, false);
                }
            }
        }
    }

    private Dictionary<string, List<string>> GetUniqueArtMapping()
    {
        Dictionary<string, List<string>> mapping = null;
        if (!Settings.UniqueIdentificationSettings.IgnoreGameUniqueArtMapping &&
            GameController.Files.UniqueItemDescriptions.EntriesList.Count != 0 &&
            GameController.Files.ItemVisualIdentities.EntriesList.Count != 0)
        {
            mapping = GetGameFileUniqueArtMapping();
        }

        var customFilePath = Path.Join(DirectoryFullName, CustomUniqueArtMappingPath);
        if (File.Exists(customFilePath))
        {
            try
            {
                mapping ??= JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(customFilePath));
            }
            catch (Exception ex)
            {
                LogError($"Unable to load custom art mapping: {ex}");
            }
        }

        mapping ??= GetEmbeddedUniqueArtMapping();
        mapping ??= [];
        return mapping.ToDictionary(x => x.Key, x => x.Value.Select(str => str.Replace('\'', '\'')).ToList());
    }

    private Dictionary<string, List<string>> GetEmbeddedUniqueArtMapping()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefaultUniqueArtMappingPath);
            if (stream == null)
            {
                if (Settings.DebugSettings.EnableDebugLogging)
                {
                    LogMessage($"Embedded stream {DefaultUniqueArtMappingPath} is missing");
                }

                return null;
            }

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(content);
        }
        catch (Exception ex)
        {
            LogError($"Unable to load embedded art mapping: {ex}");
            return null;
        }
    }

    private Dictionary<string, List<string>> GetGameFileUniqueArtMapping()
    {
        GameController.Files.UniqueItemDescriptions.ReloadIfEmptyOrZero();

        return GameController.Files.ItemVisualIdentities.EntriesList.Where(x => x.ArtPath != null)
            .GroupJoin(GameController.Files.UniqueItemDescriptions.EntriesList.Where(x => x.ItemVisualIdentity != null),
                x => x,
                x => x.ItemVisualIdentity, (ivi, descriptions) => (ivi.ArtPath, descriptions: descriptions.ToList()))
            .GroupBy(x => x.ArtPath, x => x.descriptions)
            .Select(x => (x.Key, Names: x
                .SelectMany(items => items)
                .Select(item => item.UniqueName?.Text)
                .Where(name => name != null)
                .Distinct()
                .ToList()))
            .Where(x => x.Names.Any())
            .ToDictionary(x => x.Key, x => x.Names);
    }

    private void UpdateLeagueList()
    {
        var leagueList = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        var playerLeague = PlayerLeague;
        if (playerLeague != null)
        {
            leagueList.Add(playerLeague);
        }

        // Автоматическое обновление списка лиг с poe.ninja
        try
        {
            var leagueListFromUrl = Utils.DownloadFromUrl("https://poe.ninja/api/data/getindexstate").Result;
            var leagueData = JsonConvert.DeserializeObject<NinjaLeagueListRootObject>(leagueListFromUrl);
            leagueList.UnionWith(leagueData.economyLeagues.Where(league => league.indexed).Select(league => league.name));
        }
        catch (Exception ex)
        {
            LogError($"Failed to download the league list: {ex}");
        }

        leagueList.Add("Standard");
        leagueList.Add("Hardcore");

        if (!leagueList.Contains(Settings.DataSourceSettings.League.Value))
        {
            Settings.DataSourceSettings.League.Value = leagueList.MaxBy(x => x == playerLeague);
        }

        Settings.DataSourceSettings.League.SetListValues(leagueList.ToList());
    }

    private string PlayerLeague
    {
        get
        {
            var playerLeague = GameController.IngameState.ServerData.League;
            if (string.IsNullOrWhiteSpace(playerLeague))
            {
                playerLeague = null;
            }
            else
            {
                if (playerLeague.StartsWith("SSF "))
                {
                    playerLeague = playerLeague["SSF ".Length..];
                }
            }

            return playerLeague;
        }
    }

    public override void Tick()
    {
        // Обрабатываем горячие клавиши
        if (Settings.ToggleOverlayKey.PressedOnce())
        {
            overlayVisible = !overlayVisible;
        }
        
        if (Settings.ForceUpdateKey.PressedOnce())
        {
            Task.Run(async () => await UpdatePriceData());
        }
        
        // Обновляем данные, если нужно
        if (!isUpdating && Settings.Enable && (DateTime.Now - lastUpdate).TotalMinutes >= Settings.UpdateInterval)
        {
            Task.Run(async () => await UpdatePriceData());
        }
    }

    public void RenderOverlay()
    {
        if (!Settings.Enable || !overlayVisible || !Settings.ShowOverlay)
            return;
        
        var ui = GameController?.Game?.IngameState?.IngameUi;
        if (ui == null || ui.InventoryPanel == null || !ui.InventoryPanel.IsVisible)
            return;
        
        // Рисуем оверлей с ценами предметов
        var graphics = Graphics;
        if (graphics == null)
            return;
        
        // Заголовок оверлея
        var headerText = $"Топ {topValuedItems.Count} предметов ({currentLeague})";
        var headerSize = graphics.MeasureText(headerText, Settings.FontSize);
        var windowWidth = headerSize.Width + 50;
        
        // Расчет ширины окна
        foreach (var item in topValuedItems)
        {
            var itemText = $"{item.Name} - {item.ChaosValue} chaos";
            var textSize = graphics.MeasureText(itemText, Settings.FontSize);
            windowWidth = Math.Max(windowWidth, textSize.Width + 20);
        }
        
        // Рисуем фон
        var bgColor = Settings.BackgroundColor.Value;
        bgColor.A = (byte)Settings.BackgroundAlpha.Value;
        var windowHeight = 40 + topValuedItems.Count * (Settings.FontSize + 5);
        
        graphics.DrawBox(overlayPosition, overlayPosition.Translate(windowWidth, windowHeight), bgColor);
        
        // Рисуем заголовок
        var headerPos = overlayPosition.Translate(10, 10);
        graphics.DrawText(headerText, Settings.FontSize, headerPos, Settings.TextColor);
        
        // Рисуем список предметов
        var itemPos = headerPos.Translate(0, Settings.FontSize + 10);
        foreach (var item in topValuedItems)
        {
            var itemText = $"{item.Name} - {item.ChaosValue} chaos";
            
            // Используем другой цвет для ценных предметов
            var textColor = item.ChaosValue >= Settings.HighValueThreshold 
                ? Settings.HighValueColor 
                : Settings.TextColor;
            
            graphics.DrawText(itemText, Settings.FontSize, itemPos, textColor);
            itemPos = itemPos.Translate(0, Settings.FontSize + 5);
        }
    }

    private async Task UpdateActiveLeague()
    {
        if (!Settings.AutoUpdateLeague)
            return;
        
        try
        {
            isUpdating = true;
            LogMessage("Обновление актуальной лиги...", 5);
            
            var response = await httpClient.GetStringAsync("https://poe2scout.com/api/leagues");
            if (string.IsNullOrEmpty(response))
                return;
            
            var leagues = JsonSerializer.Deserialize<List<LeagueInfo>>(response);
            if (leagues == null || leagues.Count == 0)
                return;
            
            // Выбираем первую не-Standard лигу
            var tempLeague = leagues.FirstOrDefault(l => l.Value != "Standard");
            if (tempLeague != null)
            {
                currentLeague = tempLeague.Value;
                LogMessage($"Установлена текущая лига: {currentLeague}", 5);
            }
            else
            {
                currentLeague = "Standard";
                LogMessage("Не найдены временные лиги, используем Standard", 5);
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка при получении лиг: {ex.Message}");
        }
        finally
        {
            isUpdating = false;
        }
    }

    private async Task UpdatePriceData()
    {
        if (isUpdating)
            return;
        
        try
        {
            isUpdating = true;
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            
            LogMessage("Начинаем обновление цен...", 5);
            
            // Очищаем старые данные
            priceData.Clear();
            
            // Получаем категории
            await GetPricesForCurrencyCategories(token);
            await GetPricesForUniqueCategories(token);
            
            // Обновляем список топ предметов для оверлея
            UpdateTopValuedItems();
            
            lastUpdate = DateTime.Now;
            LogMessage($"Обновление цен завершено. Получено {priceData.Count} предметов.", 5);
        }
        catch (Exception ex)
        {
            LogError($"Ошибка при обновлении цен: {ex.Message}");
        }
        finally
        {
            isUpdating = false;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    private async Task GetPricesForCurrencyCategories(CancellationToken token)
    {
        try
        {
            if (!Settings.ShowCurrency && !Settings.ShowFragments && !Settings.ShowRunes)
                return;
            
            // Получаем список категорий валюты
            var categoriesResponse = await httpClient.GetStringAsync("https://poe2scout.com/api/items/categories");
            var categories = JsonSerializer.Deserialize<CategoryResponse>(categoriesResponse);
            
            if (categories == null || categories.Currency_categories == null)
                return;
            
            foreach (var category in categories.Currency_categories)
            {
                // Проверяем настройки фильтрации
                if (category.ApiId == "currency" && !Settings.ShowCurrency)
                    continue;
                
                if (category.ApiId == "fragments" && !Settings.ShowFragments)
                    continue;
                
                if (category.ApiId == "runes" && !Settings.ShowRunes)
                    continue;
                
                // Получаем предметы категории
                var url = $"https://poe2scout.com/api/items/currency/{category.ApiId}?page=1&perPage=100&league={Uri.EscapeDataString(currentLeague)}";
                var response = await httpClient.GetStringAsync(url);
                
                var itemsResponse = JsonSerializer.Deserialize<ItemsResponse>(response);
                if (itemsResponse?.Items == null)
                    continue;
                
                foreach (var item in itemsResponse.Items)
                {
                    if (token.IsCancellationRequested)
                        return;
                    
                    var categoryName = categoryMapping.ContainsKey(category.ApiId) 
                        ? categoryMapping[category.ApiId] 
                        : category.Label;
                    
                    priceData[item.Text] = new PriceInfo
                    {
                        Name = item.Text,
                        Category = categoryName,
                        ChaosValue = item.CurrentPrice
                    };
                }
                
                LogMessage($"Загружено {itemsResponse.Items.Count} предметов категории {category.Label}", 10);
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка при получении данных валюты: {ex.Message}");
        }
    }

    private async Task GetPricesForUniqueCategories(CancellationToken token)
    {
        try
        {
            if (!Settings.ShowUniqueItems)
                return;
            
            // Получаем список категорий уникальных предметов
            var categoriesResponse = await httpClient.GetStringAsync("https://poe2scout.com/api/items/categories");
            var categories = JsonSerializer.Deserialize<CategoryResponse>(categoriesResponse);
            
            if (categories == null || categories.Unique_categories == null)
                return;
            
            foreach (var category in categories.Unique_categories)
            {
                // Получаем предметы категории
                var url = $"https://poe2scout.com/api/items/unique/{category.ApiId}?page=1&perPage=100&league={Uri.EscapeDataString(currentLeague)}";
                var response = await httpClient.GetStringAsync(url);
                
                var itemsResponse = JsonSerializer.Deserialize<ItemsResponse>(response);
                if (itemsResponse?.Items == null)
                    continue;
                
                foreach (var item in itemsResponse.Items)
                {
                    if (token.IsCancellationRequested)
                        return;
                    
                    var categoryName = categoryMapping.ContainsKey(category.ApiId) 
                        ? categoryMapping[category.ApiId] 
                        : category.Label;
                    
                    var fullName = $"{item.Name} {item.Type}";
                    
                    priceData[fullName] = new PriceInfo
                    {
                        Name = fullName,
                        Category = categoryName,
                        ChaosValue = item.CurrentPrice
                    };
                }
                
                LogMessage($"Загружено {itemsResponse.Items.Count} предметов категории {category.Label}", 10);
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка при получении данных уникальных предметов: {ex.Message}");
        }
    }

    private void UpdateTopValuedItems()
    {
        topValuedItems.Clear();
        
        // Выбираем топ N предметов по цене
        var topItems = priceData.Values
            .OrderByDescending(x => x.ChaosValue)
            .Take(Settings.ItemsToShow)
            .ToList();
        
        foreach (var item in topItems)
        {
            topValuedItems.Add(new ItemOverlayInfo
            {
                Name = item.Name,
                ChaosValue = item.ChaosValue,
                Category = item.Category
            });
        }
    }

    public override void OnClose()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        httpClient?.Dispose();
        base.OnClose();
    }

    private void LogMessage(string message, int logLevelRequired = 1)
    {
        if (Settings.Debug)
            LogMessage(message, 5, Color.White);
    }

    private new void LogError(string message)
    {
        LogMessage(message, 5, Color.Red);
    }
}