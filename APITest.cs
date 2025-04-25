using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.IO;

namespace RENP.APITest
{
    public class TestApi
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string league = "Dawn of the Hunt"; // Текущая лига
        
        public static async Task RunTest()
        {
            Console.WriteLine("=== НАЧАЛО ТЕСТИРОВАНИЯ API ===");
            Console.WriteLine($"Тестирование для лиги: {league}");
            
            try
            {
                // Тестируем API для валют
                await TestCurrencyApi();
                
                // Проверяем наличие Divine Orb в данных
                await FindDivineOrb();
                
                Console.WriteLine("=== ТЕСТИРОВАНИЕ ЗАВЕРШЕНО ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ОШИБКА: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
        private static async Task TestCurrencyApi()
        {
            Console.WriteLine("\n--- Тестирование API для валют ---");
            
            var url = $"https://poe2scout.com/api/items/currency?page=1&per_page=25&league={Uri.EscapeDataString(league)}";
            Console.WriteLine($"URL запроса: {url}");
            
            try
            {
                var response = await client.GetStringAsync(url);
                Console.WriteLine("Получен ответ от API");
                
                // Сохраняем ответ в файл для дальнейшего анализа
                var filePath = "currency_api_response.json";
                File.WriteAllText(filePath, response);
                Console.WriteLine($"Ответ сохранен в файл: {filePath}");
                
                // Десериализуем и анализируем результат
                var result = JsonConvert.DeserializeObject<dynamic>(response);
                
                Console.WriteLine($"Всего страниц: {result.pages}");
                Console.WriteLine($"Текущая страница: {result.current_page}");
                Console.WriteLine($"Количество элементов на странице: {result.items.Count}");
                
                Console.WriteLine("\nПервые 5 элементов в списке валют:");
                for (int i = 0; i < Math.Min(5, result.items.Count); i++)
                {
                    var item = result.items[i];
                    Console.WriteLine($"- {item.type}: {item.latest_price?.nominal_price}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запросе API валют: {ex.Message}");
            }
        }
        
        private static async Task FindDivineOrb()
        {
            Console.WriteLine("\n--- Поиск Divine Orb в API ---");
            
            // Список возможных имен Divine Orb
            var divineNames = new[] { "Divine Orb", "Divine", "divine orb", "divine" };
            
            try
            {
                var allCurrencies = new List<dynamic>();
                int page = 1;
                int totalPages = 1;
                
                // Получаем все страницы с валютами
                do
                {
                    var url = $"https://poe2scout.com/api/items/currency?page={page}&per_page=25&league={Uri.EscapeDataString(league)}";
                    var response = await client.GetStringAsync(url);
                    var result = JsonConvert.DeserializeObject<dynamic>(response);
                    
                    totalPages = result.pages;
                    
                    foreach (var item in result.items)
                    {
                        allCurrencies.Add(item);
                    }
                    
                    Console.WriteLine($"Загружена страница {page} из {totalPages}, получено {result.items.Count} элементов");
                    page++;
                } while (page <= totalPages);
                
                Console.WriteLine($"\nВсего загружено валют: {allCurrencies.Count}");
                
                // Ищем Divine Orb
                Console.WriteLine("\nИщем Divine Orb по точному совпадению имени:");
                bool found = false;
                foreach (var name in divineNames)
                {
                    foreach (var currency in allCurrencies)
                    {
                        string type = currency.type;
                        if (string.Equals(type, name, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"НАЙДЕНО! Имя: {currency.type}, Цена: {currency.latest_price?.nominal_price}");
                            found = true;
                        }
                    }
                }
                
                if (!found)
                {
                    Console.WriteLine("Divine Orb не найден по точному совпадению. Ищем по частичному совпадению:");
                    
                    foreach (var currency in allCurrencies)
                    {
                        string type = currency.type;
                        if (divineNames.Any(name => type.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            Console.WriteLine($"НАЙДЕНО! Имя: {currency.type}, Цена: {currency.latest_price?.nominal_price}");
                            found = true;
                        }
                    }
                }
                
                if (!found)
                {
                    Console.WriteLine("Divine Orb не найден в данных API.");
                    
                    Console.WriteLine("\nСписок всех доступных валют:");
                    foreach (var currency in allCurrencies)
                    {
                        Console.WriteLine($"- {currency.type}: {currency.latest_price?.nominal_price}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске Divine Orb: {ex.Message}");
            }
        }
    }
} 