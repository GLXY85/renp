using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;

namespace ApiTester
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static string league = "Standard"; // Начальное значение, будет обновлено при запуске

        static async Task Main(string[] args)
        {
            // Устанавливаем кодировку консоли для правильного отображения кириллицы
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            
            try
            {
                // ASCII-art заголовок для лучшей читаемости
                Console.WriteLine("===========================================");
                Console.WriteLine("||  POE2SCOUT.COM API DIAGNOSTIC TEST   ||");
                Console.WriteLine("===========================================");
                
                // Настройка HttpClient с необходимыми заголовками
                SetupHttpClient();
                
                // 0. Получаем список лиг и выбираем актуальную
                await GetActiveLeague();
                Console.WriteLine($"Выбрана лига для тестирования: {league}\n");
                
                // 1. Базовый тест доступности сайта 
                await TestWebsiteAvailability();
                
                // 2. Получение списка категорий
                await TestCategoriesEndpoint();
                
                // 3. Тест поиска Divine Orb с правильным форматом URL
                await TestDivineOrbSearch();
                
                // 4. Тестируем категории предметов согласно API-документации
                await TestItemCategories();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ОБЩАЯ ОШИБКА: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
        
        private static void SetupHttpClient()
        {
            Console.WriteLine("НАСТРОЙКА HTTP-КЛИЕНТА");
            Console.WriteLine("----------------------------");
            
            // Очищаем заголовки
            client.DefaultRequestHeaders.Clear();
            
            // Добавляем заголовки
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            client.DefaultRequestHeaders.Add("Referer", "https://poe2scout.com/");
            client.DefaultRequestHeaders.Add("Origin", "https://poe2scout.com");
            
            // Устанавливаем таймаут - увеличиваем до 60 секунд
            client.Timeout = TimeSpan.FromSeconds(60);
            
            Console.WriteLine("Заголовки настроены.");
            Console.WriteLine($"- User-Agent: {client.DefaultRequestHeaders.UserAgent}");
            Console.WriteLine($"- Таймаут: {client.Timeout.TotalSeconds} секунд");
            Console.WriteLine();
        }

        private static async Task GetActiveLeague()
        {
            Console.WriteLine("0. ПОЛУЧЕНИЕ АКТУАЛЬНОЙ ЛИГИ");
            Console.WriteLine("----------------------------");
            
            try
            {
                string url = "https://poe2scout.com/api/leagues";
                Console.WriteLine($"Запрос списка лиг: {url}");
                
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Список лиг получен");
                    
                    // Сохраняем ответ в файл
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "leagues.json");
                    File.WriteAllText(filePath, content);
                    
                    using (JsonDocument doc = JsonDocument.Parse(content))
                    {
                        var leaguesArray = doc.RootElement;
                        
                        if (leaguesArray.ValueKind == JsonValueKind.Array && leaguesArray.GetArrayLength() > 0)
                        {
                            Console.WriteLine("Доступные лиги:");
                            
                            // Временная работающая лига будет первой не-Standard лигой
                            string temporaryLeague = "Standard";
                            
                            foreach (var leagueElement in leaguesArray.EnumerateArray())
                            {
                                if (leagueElement.TryGetProperty("value", out var valueElement))
                                {
                                    string leagueName = valueElement.GetString();
                                    
                                    // Если встречаем не-Standard лигу и еще не нашли временную, запоминаем её
                                    if (leagueName != "Standard" && temporaryLeague == "Standard")
                                    {
                                        temporaryLeague = leagueName;
                                    }
                                    
                                    double divinePrice = 0;
                                    if (leagueElement.TryGetProperty("divinePrice", out var divinePriceElement))
                                    {
                                        divinePrice = divinePriceElement.GetDouble();
                                    }
                                    
                                    Console.WriteLine($"- {leagueName} (Divine Orb: {divinePrice} chaos)");
                                }
                            }
                            
                            // Используем первую временную лигу вместо Standard
                            if (temporaryLeague != "Standard")
                            {
                                league = temporaryLeague;
                                Console.WriteLine($"\nВыбрана текущая лига: {league}");
                            }
                            else
                            {
                                Console.WriteLine("\nНе найдено активных временных лиг, используем Standard");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Формат ответа не соответствует ожидаемому (массив лиг)");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Ошибка при получении списка лиг: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении списка лиг: {ex.Message}");
                Console.WriteLine("Используем лигу Standard по умолчанию");
            }
            
            Console.WriteLine();
        }
        
        private static async Task TestWebsiteAvailability()
        {
            Console.WriteLine("1. ПРОВЕРКА ДОСТУПНОСТИ САЙТА");
            Console.WriteLine("----------------------------");
            
            try
            {
                Console.WriteLine("Отправка запроса на https://poe2scout.com/");
                var response = await client.GetAsync("https://poe2scout.com/");
                Console.WriteLine($"Сайт отвечает со статусом: {(int)response.StatusCode} - {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Длина ответа: {content.Length} символов");
                    
                    Console.WriteLine("Заголовки ответа:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"- {header.Key}: {string.Join(", ", header.Value)}");
                    }
                }
                else
                {
                    Console.WriteLine($"Сайт недоступен. Код статуса: {(int)response.StatusCode} - {response.StatusCode}");
                }
                
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке доступности сайта: {ex.Message}");
                Console.WriteLine($"Трассировка стека: {ex.StackTrace}");
                Console.WriteLine();
            }
        }
        
        private static async Task TestCategoriesEndpoint()
        {
            Console.WriteLine("\n2. ТЕСТ ЭНДПОИНТА КАТЕГОРИЙ");
            Console.WriteLine("----------------------------");
            
            string url = "https://poe2scout.com/api/items/categories";
            Console.WriteLine($"Проверка URL: {url}");
            
            try
            {
                var response = await client.GetAsync(url);
                Console.WriteLine($"Статус: {(int)response.StatusCode} - {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ответ получен успешно! Длина: {content.Length} символов");
                    
                    // Сохраняем ответ в файл
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "categories_response.json");
                    File.WriteAllText(filePath, content);
                    Console.WriteLine($"Ответ сохранен в файл: {filePath}");
                    
                    // Анализируем JSON
                    if (!string.IsNullOrEmpty(content) && content.Length > 10)
                    {
                        using (JsonDocument doc = JsonDocument.Parse(content))
                        {
                            var root = doc.RootElement;
                            
                            if (root.TryGetProperty("currency_categories", out var currencyCategories))
                            {
                                Console.WriteLine("\nНайдены категории валют:");
                                foreach (var category in currencyCategories.EnumerateArray())
                                {
                                    if (category.TryGetProperty("label", out var labelElement))
                                    {
                                        string label = labelElement.GetString();
                                        string apiId = category.GetProperty("apiId").GetString();
                                        Console.WriteLine($"- {label} (apiId: {apiId})");
                                    }
                                }
                            }
                            
                            if (root.TryGetProperty("unique_categories", out var uniqueCategories))
                            {
                                Console.WriteLine("\nНайдены категории уникальных предметов:");
                                foreach (var category in uniqueCategories.EnumerateArray())
                                {
                                    if (category.TryGetProperty("label", out var labelElement))
                                    {
                                        string label = labelElement.GetString();
                                        string apiId = category.GetProperty("apiId").GetString();
                                        Console.WriteLine($"- {label} (apiId: {apiId})");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Ответ пуст или слишком короткий для JSON.");
                        Console.WriteLine($"Содержимое ответа: {content}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                if (ex is HttpRequestException httpEx)
                {
                    Console.WriteLine($"Статус HTTP: {httpEx.StatusCode}");
                }
                Console.WriteLine($"Трассировка стека: {ex.StackTrace}");
            }
            
            Console.WriteLine();
        }
        
        private static async Task TestDivineOrbSearch()
        {
            Console.WriteLine("\n3. ТЕСТ ПОИСКА DIVINE ORB");
            Console.WriteLine("----------------------------");
            
            // Согласно документации API, верный формат для поиска валюты
            string url = $"https://poe2scout.com/api/items/currency/currency?search=divine%20orb&page=1&perPage=25&league={Uri.EscapeDataString(league)}";
            Console.WriteLine($"URL: {url}");
            
            try
            {
                Console.WriteLine("Отправка запроса...");
                var response = await client.GetAsync(url);
                Console.WriteLine($"Статус: {(int)response.StatusCode} - {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ответ получен успешно! Длина: {content.Length} символов");
                    
                    // Сохраняем ответ в файл
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "divine_orb_search.json");
                    File.WriteAllText(filePath, content);
                    Console.WriteLine($"Ответ сохранен в файл: {filePath}");
                    
                    // Анализируем JSON
                    if (!string.IsNullOrEmpty(content) && content.Length > 10)
                    {
                        using (JsonDocument doc = JsonDocument.Parse(content))
                        {
                            var root = doc.RootElement;
                            
                            // Выводим структуру ответа
                            Console.WriteLine("Структура ответа JSON:");
                            foreach (var property in root.EnumerateObject())
                            {
                                Console.WriteLine($"- {property.Name} (Тип: {property.Value.ValueKind})");
                            }
                            
                            if (root.TryGetProperty("items", out var itemsElement) && 
                                itemsElement.ValueKind == JsonValueKind.Array)
                            {
                                int count = itemsElement.GetArrayLength();
                                Console.WriteLine($"Найдено {count} элементов");
                                
                                bool divineFound = false;
                                foreach (var item in itemsElement.EnumerateArray())
                                {
                                    if (item.TryGetProperty("text", out var textElement) && 
                                        textElement.GetString().Contains("Divine Orb", StringComparison.OrdinalIgnoreCase))
                                    {
                                        divineFound = true;
                                        Console.WriteLine("\nNAYDEN DIVINE ORB! Detali:");
                                        
                                        // Выводим информацию о Divine Orb
                                        foreach (var property in item.EnumerateObject())
                                        {
                                            Console.WriteLine($"- {property.Name}: {property.Value}");
                                        }
                                        
                                        // Извлекаем цену
                                        if (item.TryGetProperty("currentPrice", out var priceElement))
                                        {
                                            double price = priceElement.GetDouble();
                                            Console.WriteLine($"\nЦена Divine Orb: {price} chaos");
                                        }
                                        else
                                        {
                                            Console.WriteLine("\nНе удалось найти цену Divine Orb");
                                        }
                                        
                                        break;
                                    }
                                }
                                
                                if (!divineFound)
                                {
                                    Console.WriteLine("Divine Orb не найден в результатах поиска");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Массив 'items' не найден в ответе");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Ответ пуст или слишком короткий для JSON.");
                        Console.WriteLine($"Содержимое ответа: {content}");
                    }
                }
                else
                {
                    Console.WriteLine($"Ошибка при запросе: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                Console.WriteLine($"Трассировка стека: {ex.StackTrace}");
            }
            
            Console.WriteLine();
        }
        
        private static async Task TestItemCategories()
        {
            Console.WriteLine("\n4. ТЕСТ КАТЕГОРИЙ ПРЕДМЕТОВ");
            Console.WriteLine("----------------------------");
            
            // Получим категории из эндпоинта /items/categories
            List<(string apiId, string label, bool isCurrency)> categories = new List<(string, string, bool)>();
            
            try
            {
                var response = await client.GetAsync("https://poe2scout.com/api/items/categories");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    using (JsonDocument doc = JsonDocument.Parse(content))
                    {
                        var root = doc.RootElement;
                        
                        // Добавляем категории валюты
                        if (root.TryGetProperty("currency_categories", out var currencyCategories))
                        {
                            foreach (var category in currencyCategories.EnumerateArray())
                            {
                                string apiId = category.GetProperty("apiId").GetString();
                                string label = category.GetProperty("label").GetString();
                                categories.Add((apiId, label, true));
                            }
                        }
                        
                        // Добавляем категории уникальных предметов
                        if (root.TryGetProperty("unique_categories", out var uniqueCategories))
                        {
                            foreach (var category in uniqueCategories.EnumerateArray())
                            {
                                string apiId = category.GetProperty("apiId").GetString();
                                string label = category.GetProperty("label").GetString();
                                categories.Add((apiId, label, false));
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Не удалось получить категории. Используем предопределенный список.");
                    categories.Add(("currency", "Валюта", true));
                    categories.Add(("fragments", "Фрагменты", true));
                    categories.Add(("weapon", "Оружие", false));
                    categories.Add(("armour", "Броня", false));
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Ошибка при получении категорий. Используем предопределенный список.");
                categories.Add(("currency", "Валюта", true));
                categories.Add(("fragments", "Фрагменты", true));
                categories.Add(("weapon", "Оружие", false));
                categories.Add(("armour", "Броня", false));
            }
            
            // Тестируем первые 5 категорий из каждого типа
            int currencyTested = 0;
            int uniqueTested = 0;
            
            foreach (var (apiId, label, isCurrency) in categories)
            {
                // Тестируем только 5 категорий каждого типа
                if (isCurrency && currencyTested >= 5) continue;
                if (!isCurrency && uniqueTested >= 5) continue;
                
                Console.WriteLine($"\nПроверка категории: {label} (apiId: {apiId})");
                
                string url;
                if (isCurrency)
                {
                    url = $"https://poe2scout.com/api/items/currency/{apiId}?page=1&perPage=10&league={Uri.EscapeDataString(league)}";
                    currencyTested++;
                }
                else
                {
                    url = $"https://poe2scout.com/api/items/unique/{apiId}?page=1&perPage=10&league={Uri.EscapeDataString(league)}";
                    uniqueTested++;
                }
                
                Console.WriteLine($"URL: {url}");
                
                try
                {
                    Console.WriteLine("Отправка запроса...");
                    var response = await client.GetAsync(url);
                    Console.WriteLine($"Статус: {(int)response.StatusCode} - {response.StatusCode}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Ответ получен успешно! Длина: {content.Length} символов");
                        
                        // Анализируем JSON
                        if (!string.IsNullOrEmpty(content) && content.Length > 10)
                        {
                            using (JsonDocument doc = JsonDocument.Parse(content))
                            {
                                var root = doc.RootElement;
                                
                                if (root.TryGetProperty("items", out var itemsElement) && 
                                    itemsElement.ValueKind == JsonValueKind.Array)
                                {
                                    int count = itemsElement.GetArrayLength();
                                    Console.WriteLine($"Найдено {count} элементов");
                                    
                                    if (count > 0)
                                    {
                                        Console.WriteLine("Первые 3 элемента:");
                                        int i = 0;
                                        foreach (var item in itemsElement.EnumerateArray())
                                        {
                                            if (i++ >= 3) break;
                                            
                                            string name = GetItemName(item);
                                            double price = GetItemPrice(item);
                                            Console.WriteLine($"- {name}: {price} chaos");
                                            
                                            // Выводим id и apiId, если есть
                                            if (item.TryGetProperty("id", out var idElement))
                                                Console.WriteLine($"  * id: {idElement}");
                                            
                                            if (item.TryGetProperty("apiId", out var apiIdElement))
                                                Console.WriteLine($"  * apiId: {apiIdElement}");
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Элементы 'items' не найдены в ответе JSON");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Ответ пуст или слишком короткий для JSON.");
                            Console.WriteLine($"Содержимое ответа: {content}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                    if (ex is HttpRequestException httpEx)
                    {
                        Console.WriteLine($"Статус HTTP: {httpEx.StatusCode}");
                    }
                }
            }
        }
        
        private static string GetItemName(JsonElement item)
        {
            // Проверяем различные поля, которые могут содержать название
            string[] fieldNames = { "text", "name", "type" };
            
            foreach (var fieldName in fieldNames)
            {
                if (item.TryGetProperty(fieldName, out var nameElement))
                {
                    string name = nameElement.GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
            }
            
            return "Неизвестно";
        }
        
        private static double GetItemPrice(JsonElement item)
        {
            // Проверяем различные поля, которые могут содержать цену
            if (item.TryGetProperty("currentPrice", out var currentElement))
            {
                return currentElement.GetDouble();
            }
            
            return 0;
        }
    }
} 