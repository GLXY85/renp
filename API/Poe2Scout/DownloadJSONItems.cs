using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RENP.API.Poe2Scout.Models;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RENP.API.Poe2Scout;

public class DataDownloader
{
    private int _updating;
    public double? DivineValue { get; set; }
    public CollectiveApiData CollectedData { get; set; }

    private class LeagueMetadata
    {
        public DateTime LastLoadTime { get; set; }
    }

    public Action<string> log { get; set; }
    public RENPSettings Settings { get; set; }
    public string DataDirectory { get; set; }

    public void StartDataReload(string league, bool forceRefresh)
    {
        log($"Getting data for {league}");

        if (Interlocked.CompareExchange(ref _updating, 1, 0) != 0)
        {
            log("Update is already in progress");
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                log("Начало обновления данных с использованием нового API.");
                
                var newData = new CollectiveApiData();
                var tryWebFirst = forceRefresh;
                var metadataPath = Path.Join(DataDirectory, league, "meta.json");
                
                if (!tryWebFirst && Settings.DataSourceSettings.AutoReload)
                {
                    tryWebFirst = await IsLocalCacheStale(metadataPath);
                }
                
                // Используем новые методы загрузки
                log($"DEBUG: Загрузка данных для лиги '{league}' (принудительное обновление: {forceRefresh})");
                
                // 1. Обновляем цену Divine Orb напрямую (самый приоритетный элемент)
                await UpdateDivineOrbPrice(league);
                log($"DEBUG: Цена Divine Orb после прямого поиска: {DivineValue}");
                
                // 2. Загружаем валюты
                await DownloadCurrencies(league);
                
                // 3. Загружаем уникальные предметы (оружие, броня, аксессуары)
                await DownloadUniqueItems(league);
                
                // 4. Загружаем специальные предметы (эссенции, талисманы и т.д.)
                await DownloadSpecialItems(league);
                
                // Обновляем метаданные о последней загрузке
                new FileInfo(metadataPath).Directory?.Create();
                await File.WriteAllTextAsync(metadataPath, JsonConvert.SerializeObject(new LeagueMetadata { LastLoadTime = DateTime.UtcNow }));

                log("Завершено обновление данных с использованием нового API.");
                
                // Теперь новые данные доступны в CollectedData
                
                log($"DEBUG: Статистика загруженных данных:");
                log($"DEBUG: - Валюты: {CollectedData?.Currency?.Count ?? 0}");
                log($"DEBUG: - Оружие: {CollectedData?.Weapons?.Count ?? 0}");
                log($"DEBUG: - Броня: {CollectedData?.Armour?.Count ?? 0}");
                log($"DEBUG: - Аксессуары: {CollectedData?.Accessories?.Count ?? 0}");
                log($"DEBUG: - Эссенции: {CollectedData?.Essences?.Count ?? 0}");
                log($"DEBUG: - Breach: {CollectedData?.Breach?.Count ?? 0}");
                log($"DEBUG: - Delirium: {CollectedData?.Delirium?.Count ?? 0}");
                log($"DEBUG: - Ritual: {CollectedData?.Ritual?.Count ?? 0}");
                log($"DEBUG: - Ultimatum: {CollectedData?.Ultimatum?.Count ?? 0}");
                
                log($"DEBUG: Цена Divine Orb: {DivineValue}");
                log("Updated CollectedData.");
            }
            catch (Exception ex)
            {
                log($"Ошибка при обновлении данных: {ex.Message}");
                log($"Стек трассировки: {ex.StackTrace}");
            }
            finally
            {
                Interlocked.Exchange(ref _updating, 0);
            }
        });
    }


    private async Task<bool> IsLocalCacheStale(string metadataPath)
    {
        if (!File.Exists(metadataPath))
        {
            return true;
        }

        try
        {
            var metadata = JsonConvert.DeserializeObject<LeagueMetadata>(await File.ReadAllTextAsync(metadataPath));
            return DateTime.UtcNow - metadata.LastLoadTime > TimeSpan.FromMinutes(Settings.DataSourceSettings.ReloadPeriod);
        }
        catch (Exception ex)
        {
            if (Settings.DebugSettings.EnableDebugLogging)
            {
                log($"Metadata loading failed: {ex}");
            }

            return true;
        }
    }

    private async Task<List<TItem>> LoadData<TItem, TPaged>(string fileName, string url, string league, bool tryWebFirst) where TPaged : class, IPaged<TItem>
    {
        var backupFile = Path.Join(DataDirectory, league, fileName);
        if (tryWebFirst)
        {
            if (await LoadPagedDataFromWeb<TItem, TPaged>(fileName, url, league, backupFile) is {} data)
            {
                return data;
            }
        }

        if (await LoadDataFromBackup<TItem>(fileName, backupFile) is {} data2)
        {
            return data2;
        }

        if (!tryWebFirst)
        {
            return await LoadPagedDataFromWeb<TItem, TPaged>(fileName, url, league, backupFile);
        }

        return null;
    }

    private async Task<List<T>> LoadDataFromBackup<T>(string fileName, string backupFile)
    {
        if (File.Exists(backupFile))
        {
            try
            {
                var data = JsonConvert.DeserializeObject<List<T>>(await File.ReadAllTextAsync(backupFile));
                return data;
            }
            catch (Exception backupEx)
            {
                if (Settings.DebugSettings.EnableDebugLogging)
                {
                    log($"{fileName} backup data load failed: {backupEx}");
                }
            }
        }
        else if (Settings.DebugSettings.EnableDebugLogging)
        {
            log($"No backup for {fileName}");
        }

        return null;
    }

    private async Task<List<TItem>> LoadPagedDataFromWeb<TItem, TPaged>(string fileName, string url, string league, string backupFile) where TPaged: class, IPaged<TItem>
    {
        try
        {
            var items = new List<TItem>();
            var page = 1;
            TPaged d=null;
            do
            {
                if (Settings.DebugSettings.EnableDebugLogging)
                {
                    log($"Downloading {fileName} ({page}/{d?.pages.ToString() ?? "?"}");
                }
                d = JsonConvert.DeserializeObject<TPaged>(await Utils.DownloadFromUrl(string.Format(url, league, page)));
                items.AddRange(d.items);
                page++;
            } while (d.current_page < d.pages);

            if (Settings.DebugSettings.EnableDebugLogging)
            {
                log($"{fileName} downloaded");
            }

            try
            {
                new FileInfo(backupFile).Directory.Create();
                await File.WriteAllTextAsync(backupFile, JsonConvert.SerializeObject(items, Formatting.Indented));
            }
            catch (Exception ex)
            {
                var errorPath = backupFile + ".error";
                new FileInfo(errorPath).Directory.Create();
                await File.WriteAllTextAsync(errorPath, ex.ToString());
                if (Settings.DebugSettings.EnableDebugLogging)
                {
                    log($"{fileName} save failed: {ex}");
                }
            }

            return items;
        }
        catch (Exception ex)
        {
            if (Settings.DebugSettings.EnableDebugLogging)
            {
                log($"{fileName} fresh data download failed: {ex}");
            }

            return null;
        }
    }

    public async Task SearchDivineOrb(string league)
    {
        try
        {
            string searchTerm = "divine orb";
            log($"DEBUG: Прямой поиск Divine Orb через API для лиги '{league}'");
            
            // Формируем URL на основе предоставленного примера
            string divineOrbSearchUrl = $"{Settings.DataSettings.ApiUrl}/api/items/currency/currency?search={Uri.EscapeDataString(searchTerm)}&page=1&perPage=25&league={Uri.EscapeDataString(league)}";
            log($"DEBUG: URL для поиска Divine Orb: {divineOrbSearchUrl}");
            
            string response = await Utils.DownloadFromUrl(divineOrbSearchUrl);
            
            if (string.IsNullOrEmpty(response))
            {
                log("DEBUG: API вернул пустой ответ при поиске Divine Orb");
                SearchDivineOrbInCollectedData(); // Попробуем поискать в уже собранных данных
                return;
            }
            
            // Сохраним ответ для отладки
            Utils.SaveToFile(response, Utils.GetLocalFileName("divine_orb_search", league));
            
            var result = JsonConvert.DeserializeObject<Currency.RootObject>(response);
            if (result?.items == null || result.items.Length == 0)
            {
                log("DEBUG: В ответе API не найдено элементов Divine Orb");
                SearchDivineOrbInCollectedData(); // Попробуем поискать в уже собранных данных
                return;
            }
            
            log($"DEBUG: Найдено {result.items.Length} элементов при поиске Divine Orb");
            
            // Ищем точное совпадение
            var divineOrb = result.items.FirstOrDefault(i => 
                (i.type?.Equals("Divine Orb", StringComparison.OrdinalIgnoreCase) == true) ||
                (i.localisation_name?.Equals("Divine Orb", StringComparison.OrdinalIgnoreCase) == true));
                
            if (divineOrb != null)
            {
                decimal price = divineOrb.latest_price?.nominal_price ?? 0;
                log($"DEBUG: Найден Divine Orb с ценой {price}");
                DivineValue = (double)price;
                return;
            }
            
            // Если нет точного совпадения, ищем частичное
            var partialMatch = result.items.FirstOrDefault(i => 
                (i.type?.IndexOf("divine", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (i.localisation_name?.IndexOf("divine", StringComparison.OrdinalIgnoreCase) >= 0));
                
            if (partialMatch != null)
            {
                decimal price = partialMatch.latest_price?.nominal_price ?? 0;
                log($"DEBUG: Найдено частичное совпадение '{partialMatch.type ?? partialMatch.localisation_name}' с ценой {price}");
                DivineValue = (double)price;
                return;
            }
            
            // Если все еще не нашли, выводим список всех найденных элементов для отладки
            log("DEBUG: Divine Orb не найден среди результатов поиска. Список найденных элементов:");
            foreach (var item in result.items)
            {
                log($"DEBUG: - {item.type ?? item.localisation_name} ({item.latest_price?.nominal_price ?? 0})");
            }
            
            // Пробуем поискать в уже собранных данных
            SearchDivineOrbInCollectedData();
        }
        catch (Exception e)
        {
            log($"Ошибка при поиске Divine Orb: {e.Message}");
            try 
            {
                // Пробуем найти в собранных данных
                SearchDivineOrbInCollectedData();
            }
            catch (Exception ex) 
            {
                log($"Ошибка при поиске Divine Orb в собранных данных: {ex.Message}");
            }
        }
    }
    
    private void SearchDivineOrbInCollectedData()
    {
        log("DEBUG: Поиск Divine Orb в уже собранных данных...");
        
        if (CollectedData.Currency == null || CollectedData.Currency.Count == 0)
        {
            log("DEBUG: Нет собранных данных о валюте для поиска Divine Orb");
            return;
        }
        
        var divineOrb = CollectedData.Currency.FirstOrDefault(i => 
            (i.type?.Equals("Divine Orb", StringComparison.OrdinalIgnoreCase) == true) ||
            (i.localisation_name?.Equals("Divine Orb", StringComparison.OrdinalIgnoreCase) == true));
            
        if (divineOrb != null)
        {
            decimal price = divineOrb.latest_price?.nominal_price ?? 0;
            log($"DEBUG: Найден Divine Orb в собранных данных с ценой {price}");
            DivineValue = (double)price;
            return;
        }
        
        var partialMatch = CollectedData.Currency.FirstOrDefault(i => 
            (i.type?.IndexOf("divine", StringComparison.OrdinalIgnoreCase) >= 0) ||
            (i.localisation_name?.IndexOf("divine", StringComparison.OrdinalIgnoreCase) >= 0));
            
        if (partialMatch != null)
        {
            decimal price = partialMatch.latest_price?.nominal_price ?? 0;
            log($"DEBUG: Найдено частичное совпадение '{partialMatch.type ?? partialMatch.localisation_name}' в собранных данных с ценой {price}");
            DivineValue = (double)price;
            return;
        }
        
        log("DEBUG: Divine Orb не найден в собранных данных");
    }

    public async Task DownloadCurrencies(string league)
    {
        try
        {
            log($"DEBUG: Загрузка данных о валютах для лиги '{league}'");
            string localFileName = Utils.GetLocalFileName("currencies", league);
            
            // Проверяем наличие локального файла
            if (Utils.CheckLocalFile(localFileName))
            {
                log($"DEBUG: Найден локальный файл валюты {localFileName}");
                string localData = Utils.LoadFromFile(localFileName);
                
                if (!string.IsNullOrEmpty(localData))
                {
                    try
                    {
                        var rootObject = JsonConvert.DeserializeObject<Currency.RootObject>(localData);
                        if (rootObject != null && rootObject.items != null && rootObject.items.Length > 0)
                        {
                            CollectedData.Currency = new List<Currency.Item>(rootObject.items);
                            log($"DEBUG: Загружено {CollectedData.Currency.Count} валют из локального файла");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        log($"Ошибка парсинга локального файла валюты: {ex.Message}");
                    }
                }
            }
            
            // Если локальный файл не найден или не валидный, загружаем с API
            log($"DEBUG: Загрузка данных валюты с API для лиги '{league}'");
            
            // Получаем первую страницу валют
            string url = $"{Settings.DataSettings.ApiUrl}/api/items/currency/currency?page=1&perPage=100&league={Uri.EscapeDataString(league)}";
            log($"DEBUG: URL загрузки валют: {url}");
            
            string response = await Utils.DownloadFromUrl(url);
            
            if (string.IsNullOrEmpty(response))
            {
                log("DEBUG: API вернул пустой ответ для валют");
                return;
            }
            
            // Сохраняем ответ в локальный файл
            Utils.SaveToFile(response, localFileName);
            
            var result = JsonConvert.DeserializeObject<Currency.RootObject>(response);
            if (result?.items != null && result.items.Length > 0)
            {
                CollectedData.Currency = new List<Currency.Item>(result.items);
                log($"DEBUG: Загружено {CollectedData.Currency.Count} валют с API");
                
                // Проверяем, есть ли еще страницы для загрузки
                if (result.current_page < result.pages)
                {
                    log($"DEBUG: Есть еще страницы с валютами. Всего страниц: {result.pages}");
                    
                    // Загружаем остальные страницы
                    for (int page = 2; page <= result.pages; page++)
                    {
                        string pageUrl = $"{Settings.DataSettings.ApiUrl}/api/items/currency/currency?page={page}&perPage=100&league={Uri.EscapeDataString(league)}";
                        log($"DEBUG: Загрузка страницы {page} валют: {pageUrl}");
                        
                        string pageResponse = await Utils.DownloadFromUrl(pageUrl);
                        if (string.IsNullOrEmpty(pageResponse))
                        {
                            log($"DEBUG: API вернул пустой ответ для страницы {page} валют");
                            continue;
                        }
                        
                        var pageResult = JsonConvert.DeserializeObject<Currency.RootObject>(pageResponse);
                        if (pageResult?.items != null && pageResult.items.Length > 0)
                        {
                            CollectedData.Currency.AddRange(pageResult.items);
                            log($"DEBUG: Добавлено {pageResult.items.Length} валют со страницы {page}. Всего: {CollectedData.Currency.Count}");
                        }
                    }
                }
                
                // Обновляем базу данных валют
                log($"DEBUG: Всего загружено {CollectedData.Currency.Count} валют");
                
                // Сохраняем полный список валют
                Utils.SaveToFile(JsonConvert.SerializeObject(new { items = CollectedData.Currency }), 
                    Utils.GetLocalFileName("all_currencies", league));
            }
            else
            {
                log("DEBUG: Нет данных валюты в ответе API или ошибка парсинга");
            }
        }
        catch (Exception e)
        {
            log($"Ошибка при загрузке валют: {e.Message}");
        }
    }

    // Общий метод для поиска предметов через API для разных категорий
    public async Task<T> SearchItemByName<T, TRoot>(string itemName, string category, string league) 
        where T : class 
        where TRoot : class, IPaged<T>
    {
        try
        {
            log($"DEBUG: Поиск предмета '{itemName}' в категории '{category}' для лиги '{league}'");
            
            // Формируем URL для поиска предмета
            string searchUrl = $"{Settings.DataSettings.ApiUrl}/api/items/{category}?search={Uri.EscapeDataString(itemName)}&page=1&perPage=25&league={Uri.EscapeDataString(league)}";
            log($"DEBUG: URL для поиска предмета: {searchUrl}");
            
            string response = await Utils.DownloadFromUrl(searchUrl);
            
            if (string.IsNullOrEmpty(response))
            {
                log($"DEBUG: API вернул пустой ответ при поиске предмета '{itemName}'");
                return null;
            }
            
            // Сохраняем ответ для отладки
            Utils.SaveToFile(response, Utils.GetLocalFileName($"{category}_{itemName}_search", league));
            
            var result = JsonConvert.DeserializeObject<TRoot>(response);
            // Получаем список элементов из result с использованием рефлексии
            var itemsProperty = typeof(TRoot).GetProperty("items");
            if (itemsProperty == null)
            {
                log($"DEBUG: Ошибка в структуре ответа API - не найдено свойство 'items'");
                return null;
            }
            
            var items = itemsProperty.GetValue(result) as IEnumerable<T>;
            if (items == null || !items.Any())
            {
                log($"DEBUG: В ответе API не найдено элементов для '{itemName}'");
                return null;
            }
            
            log($"DEBUG: Найдено {items.Count()} элементов при поиске '{itemName}'");
            
            // Ищем предмет по полям "type" или "name" или "localisation_name" (в зависимости от типа)
            var properties = typeof(T).GetProperties();
            var typeProperty = properties.FirstOrDefault(p => p.Name.Equals("type", StringComparison.OrdinalIgnoreCase));
            var nameProperty = properties.FirstOrDefault(p => p.Name.Equals("name", StringComparison.OrdinalIgnoreCase));
            var localisationNameProperty = properties.FirstOrDefault(p => p.Name.Equals("localisation_name", StringComparison.OrdinalIgnoreCase));
            
            foreach (var item in items)
            {
                string itemType = typeProperty?.GetValue(item) as string;
                string itemName2 = nameProperty?.GetValue(item) as string;
                string localisationName = localisationNameProperty?.GetValue(item) as string;
                
                // Ищем точное совпадение
                if ((itemType != null && itemType.Equals(itemName, StringComparison.OrdinalIgnoreCase)) ||
                    (itemName2 != null && itemName2.Equals(itemName, StringComparison.OrdinalIgnoreCase)) ||
                    (localisationName != null && localisationName.Equals(itemName, StringComparison.OrdinalIgnoreCase)))
                {
                    log($"DEBUG: Найдено точное совпадение для '{itemName}'");
                    return item;
                }
            }
            
            // Если нет точного совпадения, ищем частичное
            foreach (var item in items)
            {
                string itemType = typeProperty?.GetValue(item) as string;
                string itemName2 = nameProperty?.GetValue(item) as string;
                string localisationName = localisationNameProperty?.GetValue(item) as string;
                
                if ((itemType != null && itemType.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (itemName2 != null && itemName2.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (localisationName != null && localisationName.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    log($"DEBUG: Найдено частичное совпадение для '{itemName}'");
                    return item;
                }
            }
            
            // Выводим список всех найденных элементов для отладки
            log($"DEBUG: Предмет '{itemName}' не найден среди результатов поиска. Список найденных элементов:");
            foreach (var item in items)
            {
                string itemType = typeProperty?.GetValue(item) as string;
                string itemName2 = nameProperty?.GetValue(item) as string;
                string localisationName = localisationNameProperty?.GetValue(item) as string;
                log($"DEBUG: - {itemType ?? itemName2 ?? localisationName ?? "Неизвестно"}");
            }
            
            return null;
        }
        catch (Exception e)
        {
            log($"Ошибка при поиске предмета '{itemName}': {e.Message}");
            return null;
        }
    }
    
    // Метод для поиска конкретной валюты
    public async Task<double?> SearchCurrencyPrice(string currencyName, string league)
    {
        try
        {
            var currency = await SearchItemByName<Currency.Item, Currency.RootObject>(
                currencyName, "currency/currency", league);
                
            if (currency != null && currency.latest_price != null)
            {
                decimal price = currency.latest_price.nominal_price;
                log($"DEBUG: Найдена валюта '{currencyName}' с ценой {price}");
                return (double)price;
            }
            
            // Если через API не нашли, ищем в собранных данных
            if (CollectedData?.Currency != null)
            {
                var properties = typeof(Currency.Item).GetProperties();
                var typeProperty = properties.FirstOrDefault(p => p.Name.Equals("type", StringComparison.OrdinalIgnoreCase));
                var localisationNameProperty = properties.FirstOrDefault(p => p.Name.Equals("localisation_name", StringComparison.OrdinalIgnoreCase));
                
                foreach (var item in CollectedData.Currency)
                {
                    string itemType = typeProperty?.GetValue(item) as string;
                    string localisationName = localisationNameProperty?.GetValue(item) as string;
                    
                    // Ищем точное совпадение
                    if ((itemType != null && itemType.Equals(currencyName, StringComparison.OrdinalIgnoreCase)) ||
                        (localisationName != null && localisationName.Equals(currencyName, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (item.latest_price != null)
                        {
                            decimal price = item.latest_price.nominal_price;
                            log($"DEBUG: Найдена валюта '{currencyName}' в собранных данных с ценой {price}");
                            return (double)price;
                        }
                    }
                }
                
                // Если нет точного совпадения, ищем частичное
                foreach (var item in CollectedData.Currency)
                {
                    string itemType = typeProperty?.GetValue(item) as string;
                    string localisationName = localisationNameProperty?.GetValue(item) as string;
                    
                    if ((itemType != null && itemType.IndexOf(currencyName, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (localisationName != null && localisationName.IndexOf(currencyName, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        if (item.latest_price != null)
                        {
                            decimal price = item.latest_price.nominal_price;
                            log($"DEBUG: Найдено частичное совпадение '{itemType ?? localisationName}' для валюты '{currencyName}' в собранных данных с ценой {price}");
                            return (double)price;
                        }
                    }
                }
            }
            
            log($"DEBUG: Валюта '{currencyName}' не найдена");
            return null;
        }
        catch (Exception e)
        {
            log($"Ошибка при поиске цены валюты '{currencyName}': {e.Message}");
            return null;
        }
    }
    
    // Метод для поиска конкретного уникального предмета
    public async Task<double?> SearchUniqueItemPrice(string itemName, string category, string league)
    {
        try
        {
            // Определяем тип объекта на основе категории
            Type itemType = null;
            Type rootType = null;
            
            switch (category.ToLower())
            {
                case "weapon":
                    itemType = typeof(Weapons.Item);
                    rootType = typeof(Weapons.RootObject);
                    break;
                case "armour":
                    itemType = typeof(Armour.Item);
                    rootType = typeof(Armour.RootObject);
                    break;
                case "accessory":
                    itemType = typeof(Accessories.Item);
                    rootType = typeof(Accessories.RootObject);
                    break;
                case "essences":
                    itemType = typeof(Essences.Item);
                    rootType = typeof(Essences.RootObject);
                    break;
                default:
                    log($"DEBUG: Неизвестная категория предметов: {category}");
                    return null;
            }
            
            // Используем reflection для вызова generic метода с правильными типами
            var method = typeof(DataDownloader).GetMethod("SearchItemByName");
            var genericMethod = method.MakeGenericMethod(itemType, rootType);
            
            var item = genericMethod.Invoke(this, new object[] { itemName, category, league });
            
            if (item != null)
            {
                // Используем reflection для доступа к цене
                var latestPriceProperty = itemType.GetProperty("latest_price");
                if (latestPriceProperty != null)
                {
                    var latestPrice = latestPriceProperty.GetValue(item);
                    if (latestPrice != null)
                    {
                        var nominalPriceProperty = latestPrice.GetType().GetProperty("nominal_price");
                        if (nominalPriceProperty != null)
                        {
                            decimal price = (decimal)nominalPriceProperty.GetValue(latestPrice);
                            log($"DEBUG: Найден предмет '{itemName}' в категории '{category}' с ценой {price}");
                            return (double)price;
                        }
                    }
                }
            }
            
            // Если через API не нашли, можно также поискать в собранных данных
            log($"DEBUG: Предмет '{itemName}' не найден в категории '{category}'");
            return null;
        }
        catch (Exception e)
        {
            log($"Ошибка при поиске цены предмета '{itemName}' категории '{category}': {e.Message}");
            return null;
        }
    }
    
    // Метод для получения данных о ценах с poe.ninja API
    public async Task<double?> GetPriceFromPoeNinja(string itemName, string itemType, string league)
    {
        try
        {
            log($"DEBUG: Получение цены для {itemName} (тип: {itemType}) из poe.ninja API");
            
            // Определяем тип API запроса на основе типа предмета
            string apiType;
            switch (itemType.ToLower())
            {
                case "currency":
                case "fragments":
                    apiType = "Currency";
                    break;
                case "essence":
                case "essences":
                    apiType = "Essence";
                    break;
                case "divination card":
                case "cards":
                    apiType = "DivinationCard";
                    break;
                case "unique weapon":
                case "weapon":
                    apiType = "UniqueWeapon";
                    break;
                case "unique armour":
                case "armour":
                    apiType = "UniqueArmour";
                    break;
                case "unique accessory":
                case "accessory":
                    apiType = "UniqueAccessory";
                    break;
                case "unique flask":
                case "flask":
                    apiType = "UniqueFlask";
                    break;
                case "unique jewel":
                case "jewel":
                    apiType = "UniqueJewel";
                    break;
                default:
                    apiType = "Currency";
                    break;
            }
            
            // Формируем URL
            string url = $"https://poe.ninja/api/data/{(apiType == "Currency" ? "currency" : "item")}overview?league={Uri.EscapeDataString(league)}&type={apiType}";
            log($"DEBUG: URL API poe.ninja: {url}");
            
            // Загружаем данные
            string response = await Utils.DownloadFromUrl(url);
            if (string.IsNullOrEmpty(response))
            {
                log($"DEBUG: API poe.ninja вернул пустой ответ");
                return null;
            }
            
            // Сохраняем ответ для отладки
            Utils.SaveToFile(response, Utils.GetLocalFileName($"poe_ninja_{itemType.ToLower()}", league));
            
            // Разбираем JSON
            JObject rootObject = JObject.Parse(response);
            
            // Находим элемент по имени
            if (rootObject["lines"] is JArray lines)
            {
                JToken matchedItem = null;
                
                // Сначала ищем точное совпадение
                foreach (JToken item in lines)
                {
                    string name = null;
                    
                    if (item["currencyTypeName"] != null)
                        name = item["currencyTypeName"].ToString();
                    else if (item["name"] != null)
                        name = item["name"].ToString();
                    
                    if (name != null && name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedItem = item;
                        log($"DEBUG: Найдено точное совпадение для {itemName} в poe.ninja API");
                        break;
                    }
                }
                
                // Если точное совпадение не найдено, ищем частичное
                if (matchedItem == null)
                {
                    foreach (JToken item in lines)
                    {
                        string name = null;
                        
                        if (item["currencyTypeName"] != null)
                            name = item["currencyTypeName"].ToString();
                        else if (item["name"] != null)
                            name = item["name"].ToString();
                        
                        if (name != null && name.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            matchedItem = item;
                            log($"DEBUG: Найдено частичное совпадение для {itemName} в poe.ninja API: {name}");
                            break;
                        }
                    }
                }
                
                // Если нашли элемент, получаем его цену
                if (matchedItem != null)
                {
                    double price = 0;
                    
                    if (matchedItem["chaosValue"] != null)
                        price = matchedItem["chaosValue"].ToObject<double>();
                    else if (matchedItem["chaosEquivalent"] != null)
                        price = matchedItem["chaosEquivalent"].ToObject<double>();
                    
                    log($"DEBUG: Цена {itemName} в poe.ninja API: {price} chaos");
                    return price;
                }
                else
                {
                    // Если предмет не найден, выводим список всех доступных предметов для отладки
                    if (Settings.DebugSettings.EnableDebugLogging)
                    {
                        log($"DEBUG: Предмет {itemName} не найден в poe.ninja API. Доступные предметы:");
                        int count = 0;
                        foreach (JToken item in lines)
                        {
                            string name = null;
                            
                            if (item["currencyTypeName"] != null)
                                name = item["currencyTypeName"].ToString();
                            else if (item["name"] != null)
                                name = item["name"].ToString();
                            
                            if (name != null)
                            {
                                log($"DEBUG: - {name}");
                                count++;
                                
                                // Ограничиваем вывод до 10 элементов
                                if (count >= 10)
                                {
                                    log($"DEBUG: ... и еще {lines.Count - 10} элементов");
                                    break;
                                }
                            }
                        }
                    }
                    
                    log($"DEBUG: Предмет {itemName} не найден в poe.ninja API");
                    return null;
                }
            }
            
            log($"DEBUG: Ошибка в структуре ответа API poe.ninja - отсутствует поле 'lines'");
            return null;
        }
        catch (Exception ex)
        {
            log($"Ошибка при получении цены из poe.ninja API: {ex.Message}");
            return null;
        }
    }
    
    // Обновленный метод для получения цены Divine Orb с использованием API poe.ninja
    public async Task UpdateDivineOrbPrice(string league)
    {
        try
        {
            // Сначала пытаемся получить цену из API poe2scout, если он доступен
            double? price = await SearchCurrencyPrice("Divine Orb", league);
            
            // Если цена не найдена в API poe2scout, пробуем poe.ninja
            if (price == null)
            {
                log("DEBUG: Не удалось получить цену Divine Orb из poe2scout API. Пробуем poe.ninja API...");
                price = await GetPriceFromPoeNinja("Divine Orb", "currency", league);
            }
            
            if (price.HasValue)
            {
                DivineValue = price;
                log($"DEBUG: Цена Divine Orb обновлена: {DivineValue}");
            }
            else
            {
                log("DEBUG: Не удалось найти цену Divine Orb ни в одном из API");
            }
        }
        catch (Exception ex)
        {
            log($"Ошибка при обновлении цены Divine Orb: {ex.Message}");
        }
    }
    
    // Общий метод для поиска цены любого предмета (с использованием обоих API)
    public async Task<double?> GetItemPrice(string itemName, string itemType, string league)
    {
        try
        {
            log($"DEBUG: Поиск цены для {itemName} (тип: {itemType})");
            
            // Сначала пытаемся найти в poe2scout API
            double? price = null;
            
            if (itemType.ToLower() == "currency")
            {
                price = await SearchCurrencyPrice(itemName, league);
            }
            else
            {
                price = await SearchUniqueItemPrice(itemName, itemType, league);
            }
            
            // Если не нашли в poe2scout, пробуем poe.ninja
            if (price == null)
            {
                log($"DEBUG: Не удалось найти цену {itemName} в poe2scout API. Пробуем poe.ninja API...");
                price = await GetPriceFromPoeNinja(itemName, itemType, league);
            }
            
            if (price.HasValue)
            {
                log($"DEBUG: Цена {itemName} найдена: {price} chaos");
                return price;
            }
            else
            {
                log($"DEBUG: Не удалось найти цену {itemName} ни в одном из API");
                return null;
            }
        }
        catch (Exception ex)
        {
            log($"Ошибка при поиске цены {itemName}: {ex.Message}");
            return null;
        }
    }

    // Обобщенный метод для загрузки любой категории предметов
    public async Task<List<TItem>> DownloadItems<TItem, TRoot>(string category, string league, int perPage = 100)
        where TRoot : class, IPaged<TItem>
    {
        try
        {
            log($"DEBUG: Загрузка предметов категории '{category}' для лиги '{league}'");
            string localFileName = Utils.GetLocalFileName($"{category}_items", league);
            
            // Проверяем наличие локального файла
            if (Utils.CheckLocalFile(localFileName))
            {
                log($"DEBUG: Найден локальный файл {localFileName}");
                string localData = Utils.LoadFromFile(localFileName);
                
                if (!string.IsNullOrEmpty(localData))
                {
                    try
                    {
                        var items = JsonConvert.DeserializeObject<List<TItem>>(localData);
                        if (items != null && items.Count > 0)
                        {
                            log($"DEBUG: Загружено {items.Count} предметов категории '{category}' из локального файла");
                            return items;
                        }
                    }
                    catch (Exception ex)
                    {
                        log($"Ошибка парсинга локального файла {category}: {ex.Message}");
                    }
                }
            }
            
            // Если локальный файл не найден или не валидный, загружаем с API
            log($"DEBUG: Загрузка предметов категории '{category}' с API для лиги '{league}'");
            
            List<TItem> allItems = new List<TItem>();
            int page = 1;
            bool hasMorePages = true;
            
            while (hasMorePages)
            {
                string url = $"{Settings.DataSettings.ApiUrl}/api/items/{category}?page={page}&perPage={perPage}&league={Uri.EscapeDataString(league)}";
                log($"DEBUG: Загрузка страницы {page} предметов категории '{category}': {url}");
                
                string response = await Utils.DownloadFromUrl(url);
                if (string.IsNullOrEmpty(response))
                {
                    log($"DEBUG: API вернул пустой ответ для страницы {page} категории '{category}'");
                    break;
                }
                
                // Для первой страницы сохраняем ответ в локальный файл (для отладки)
                if (page == 1)
                {
                    Utils.SaveToFile(response, Utils.GetLocalFileName($"{category}_response_p1", league));
                }
                
                var result = JsonConvert.DeserializeObject<TRoot>(response);
                
                // Получаем список элементов из result с использованием рефлексии
                var itemsProperty = typeof(TRoot).GetProperty("items");
                if (itemsProperty == null)
                {
                    log($"DEBUG: Ошибка в структуре ответа API - не найдено свойство 'items'");
                    break;
                }
                
                // Получаем информацию о пагинации
                var currentPageProperty = typeof(TRoot).GetProperty("current_page");
                var pagesProperty = typeof(TRoot).GetProperty("pages");
                
                int currentPage = 1;
                int totalPages = 1;
                
                if (currentPageProperty != null && pagesProperty != null)
                {
                    currentPage = (int)currentPageProperty.GetValue(result);
                    totalPages = (int)pagesProperty.GetValue(result);
                    
                    // Проверяем, есть ли еще страницы
                    hasMorePages = currentPage < totalPages;
                }
                else
                {
                    hasMorePages = false;
                }
                
                // Получаем элементы текущей страницы
                var items = itemsProperty.GetValue(result) as IEnumerable<TItem>;
                if (items != null)
                {
                    var itemsList = items.ToList();
                    allItems.AddRange(itemsList);
                    log($"DEBUG: Добавлено {itemsList.Count} предметов категории '{category}' со страницы {page}. Всего: {allItems.Count}");
                }
                
                page++;
            }
            
            // Сохраняем все предметы в локальный файл
            if (allItems.Count > 0)
            {
                Utils.SaveToFile(JsonConvert.SerializeObject(allItems), localFileName);
                log($"DEBUG: Сохранено {allItems.Count} предметов категории '{category}' в локальный файл");
            }
            
            return allItems;
        }
        catch (Exception e)
        {
            log($"Ошибка при загрузке предметов категории '{category}': {e.Message}");
            return new List<TItem>();
        }
    }
    
    // Метод для загрузки уникальных предметов (оружие, броня, аксессуары)
    public async Task DownloadUniqueItems(string league)
    {
        try
        {
            log($"DEBUG: Начало загрузки уникальных предметов для лиги '{league}'");
            
            // Загружаем оружие
            var weapons = await DownloadItems<Weapons.Item, Weapons.RootObject>("weapon", league);
            if (weapons.Count > 0)
            {
                CollectedData.Weapons = weapons;
                log($"DEBUG: Загружено {weapons.Count} уникальных оружий");
            }
            
            // Загружаем броню
            var armour = await DownloadItems<Armour.Item, Armour.RootObject>("armour", league);
            if (armour.Count > 0)
            {
                CollectedData.Armour = armour;
                log($"DEBUG: Загружено {armour.Count} уникальных доспехов");
            }
            
            // Загружаем аксессуары
            var accessories = await DownloadItems<Accessories.Item, Accessories.RootObject>("accessory", league);
            if (accessories.Count > 0)
            {
                CollectedData.Accessories = accessories;
                log($"DEBUG: Загружено {accessories.Count} уникальных аксессуаров");
            }
            
            log($"DEBUG: Завершена загрузка уникальных предметов для лиги '{league}'");
        }
        catch (Exception e)
        {
            log($"Ошибка при загрузке уникальных предметов: {e.Message}");
        }
    }
    
    // Метод для загрузки различных дополнительных предметов (эссенции, талисманы, и т.д.)
    public async Task DownloadSpecialItems(string league)
    {
        try
        {
            log($"DEBUG: Начало загрузки особых предметов для лиги '{league}'");
            
            // Загружаем эссенции
            var essences = await DownloadItems<Essences.Item, Essences.RootObject>("essences", league);
            if (essences.Count > 0)
            {
                CollectedData.Essences = essences;
                log($"DEBUG: Загружено {essences.Count} эссенций");
            }
            
            // Загружаем предметы из Breach
            var breach = await DownloadItems<Breach.Item, Breach.RootObject>("breachcatalyst", league);
            if (breach.Count > 0)
            {
                CollectedData.Breach = breach;
                log($"DEBUG: Загружено {breach.Count} предметов Breach");
            }
            
            // Загружаем предметы Delirium
            var delirium = await DownloadItems<Delirium.Item, Delirium.RootObject>("deliriuminstill", league);
            if (delirium.Count > 0)
            {
                CollectedData.Delirium = delirium;
                log($"DEBUG: Загружено {delirium.Count} предметов Delirium");
            }
            
            // Загружаем предметы Ritual
            var ritual = await DownloadItems<Ritual.Item, Ritual.RootObject>("ritual", league);
            if (ritual.Count > 0)
            {
                CollectedData.Ritual = ritual;
                log($"DEBUG: Загружено {ritual.Count} предметов Ritual");
            }
            
            // Загружаем предметы Ultimatum
            var ultimatum = await DownloadItems<Ultimatum.Item, Ultimatum.RootObject>("ultimatum", league);
            if (ultimatum.Count > 0)
            {
                CollectedData.Ultimatum = ultimatum;
                log($"DEBUG: Загружено {ultimatum.Count} предметов Ultimatum");
            }
            
            log($"DEBUG: Завершена загрузка особых предметов для лиги '{league}'");
        }
        catch (Exception e)
        {
            log($"Ошибка при загрузке особых предметов: {e.Message}");
        }
    }
}