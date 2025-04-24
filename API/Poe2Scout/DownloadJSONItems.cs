using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RENP.API.Poe2Scout.Models;

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
                log("Gathering Data from Poe.Ninja.");

                var newData = new CollectiveApiData();
                var tryWebFirst = forceRefresh;
                var metadataPath = Path.Join(DataDirectory, league, "meta.json");
                if (!tryWebFirst && Settings.DataSourceSettings.AutoReload)
                {
                    tryWebFirst = await IsLocalCacheStale(metadataPath);
                }

                newData.Currency = await LoadData<Currency.Item, Currency.RootObject>("Currency.json", "https://poe2scout.com/api/items/currency?page={1}&per_page=25&league={0}", league, tryWebFirst);
                newData.Breach = await LoadData<Breach.Item, Breach.RootObject>("Breach.json", "https://poe2scout.com/api/items/breachcatalyst?page={1}&per_page=25&league={0}", league, tryWebFirst);
                newData.Weapons = await LoadData<Weapons.Item, Weapons.RootObject>("Weapons.json", "https://poe2scout.com/api/items/weapon?page={1}&per_page=25&league={0}", league, tryWebFirst);
                newData.Armour = await LoadData<Armour.Item, Armour.RootObject>("Armour.json", "https://poe2scout.com/api/items/armour?page={1}&per_page=25&league={0}", league, tryWebFirst);
                newData.Accessories = await LoadData<Accessories.Item, Accessories.RootObject>("Accessories.json", "https://poe2scout.com/api/items/accessory?page={1}&per_page=25&league={0}", league, tryWebFirst);
                newData.Delirium = await LoadData<Delirium.Item, Delirium.RootObject>("Delirium.json", "https://poe2scout.com/api/items/deliriuminstill?page={1}&per_page=25&league={0}", league, tryWebFirst);
                newData.Essences = await LoadData<Essences.Item, Essences.RootObject>("Essences.json", "https://poe2scout.com/api/items/essences?page={1}&per_page=25&league={0}", league, tryWebFirst);
                newData.Ritual = await LoadData<Ritual.Item, Ritual.RootObject>("Ritual.json", "https://poe2scout.com/api/items/ritual?page={1}&per_page=25&league={0}", league, tryWebFirst);
                newData.Ultimatum = await LoadData<Ultimatum.Item, Ultimatum.RootObject>("Ultimatum.json", "https://poe2scout.com/api/items/ultimatum?page={1}&per_page=25&league={0}", league, tryWebFirst);

                new FileInfo(metadataPath).Directory?.Create();
                await File.WriteAllTextAsync(metadataPath, JsonConvert.SerializeObject(new LeagueMetadata { LastLoadTime = DateTime.UtcNow }));

                log("Finished Gathering Data from Poe.Ninja.");
                CollectedData = newData;
                DivineValue = CollectedData.Currency.Find(x => x.type == "Divine Orb")?.latest_price?.nominal_price;
                log("Updated CollectedData.");
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
}