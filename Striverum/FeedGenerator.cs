using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Striverum
{
    public enum GameFilter
    {
        GGS
    }
    public enum FeedFilter
    {
        Featured,
        Recent,
        Popular,
        None
    }
    public enum TypeFilter
    {
        Mods,
        WiPs,
        Sounds
    }
    public static class FeedGenerator
    {
        private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 20
        });
        private static Dictionary<string, GameBananaModList> feed = new();
        private static readonly object feedLock = new();
        public static bool error;
        public static Exception exception;
        public static GameBananaModList CurrentFeed;
        public static double GetHeader(this HttpResponseMessage request, string key)
        {
            IEnumerable<string> keys = null;
            if (!request.Headers.TryGetValues(key, out keys))
                return -1;
            return Double.Parse(keys.First());
        }
        private static Dictionary<string, (List<GameBananaRecord> Records, DateTime TimeFetched)> searchFilteredCache = new();
        public static void ClearCache()
        {
            if (feed != null)
                feed.Clear();
            if (searchFilteredCache != null)
            {
                lock (searchFilteredCache)
                {
                    searchFilteredCache.Clear();
                }
            }
        }

        public static bool MatchesCategory(GameBananaRecord record, GameBananaCategory category, GameBananaCategory subcategory)
        {
            if (record == null) return false;

            if (subcategory != null && subcategory.ID != null)
            {
                return (record.Category != null && (record.Category.ID == subcategory.ID || string.Equals(record.Category.Name, subcategory.Name, StringComparison.OrdinalIgnoreCase))) ||
                       (record.RootCategory != null && (record.RootCategory.ID == subcategory.ID || string.Equals(record.RootCategory.Name, subcategory.Name, StringComparison.OrdinalIgnoreCase)));
            }

            if (category != null && category.ID != null)
            {
                return (record.RootCategory != null && (record.RootCategory.ID == category.ID || string.Equals(record.RootCategory.Name, category.Name, StringComparison.OrdinalIgnoreCase))) ||
                       (record.Category != null && (record.Category.ID == category.ID || string.Equals(record.Category.Name, category.Name, StringComparison.OrdinalIgnoreCase)));
            }

            return true;
        }

        public static async Task GetFeed(int page, GameFilter game, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage, bool nsfw, string search, bool zs, bool colorz)
        {
            error = false;
            bool isSearchWithCategory = !string.IsNullOrWhiteSpace(search) && ((category != null && category.ID != null) || (subcategory != null && subcategory.ID != null));

            if (isSearchWithCategory)
            {
                string searchKey = $"{type}_{category?.ID}_{subcategory?.ID}_{search.Trim().ToLowerInvariant()}_{nsfw}";
                lock (searchFilteredCache)
                {
                    if (searchFilteredCache.ContainsKey(searchKey) && (DateTime.UtcNow - searchFilteredCache[searchKey].TimeFetched).TotalMinutes < 15)
                    {
                        var cachedList = searchFilteredCache[searchKey].Records;
                        var pagedList = cachedList.Skip((page - 1) * perPage).Take(perPage).ToList();
                        CurrentFeed = new GameBananaModList
                        {
                            Records = new ObservableCollection<GameBananaRecord>(pagedList),
                            TotalPages = Math.Max(1, Math.Ceiling(cachedList.Count / (double)perPage))
                        };
                        return;
                    }
                }

                CurrentFeed = new();
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        var allMatching = new List<GameBananaRecord>();
                        var searchBaseUrl = "https://gamebanana.com/apiv6/";
                        switch (type)
                        {
                            case TypeFilter.Mods:
                                searchBaseUrl += "Mod/";
                                break;
                            case TypeFilter.Sounds:
                                searchBaseUrl += "Sound/";
                                break;
                            case TypeFilter.WiPs:
                                searchBaseUrl += "Wip/";
                                break;
                        }
                        searchBaseUrl += $"ByName?_sName=*{search}*&_idGameRow=11534&" +
                            $"_csvProperties=_sName,_sModelName,_sProfileUrl,_aSubmitter,_tsDateUpdated,_tsDateAdded,_aPreviewMedia,_sText,_sDescription,_aCategory,_aRootCategory,_aGame,_nViewCount," +
                            $"_nLikeCount,_nDownloadCount,_aFiles,_aModManagerIntegrations,_bIsNsfw,_aAlternateFileSources&_nPerpage=50";
                        if (!nsfw)
                            searchBaseUrl += "&_aArgs[]=_sbIsNsfw = false";

                        var firstPageUrl = $"{searchBaseUrl}&_nPage=1";
                        var searchResponse = await httpClient.GetAsync(firstPageUrl);
                        if (searchResponse.IsSuccessStatusCode)
                        {
                            var json = await searchResponse.Content.ReadAsStringAsync();
                            if (json.TrimStart().StartsWith("["))
                            {
                                var rawRecords = JsonSerializer.Deserialize<List<GameBananaRecord>>(json);
                                if (rawRecords != null)
                                {
                                    allMatching.AddRange(rawRecords.Where(r => MatchesCategory(r, category, subcategory)));
                                }
                            }
                        }

                        var numRecords = searchResponse.GetHeader("X-GbApi-Metadata_nRecordCount");
                        if (numRecords > 50)
                        {
                            var totalGbPages = (int)Math.Min(Math.Ceiling(numRecords / 50.0), 6);
                            var pageTasks = new List<Task<string>>();
                            for (int p = 2; p <= totalGbPages; p++)
                            {
                                string pageUrl = $"{searchBaseUrl}&_nPage={p}";
                                pageTasks.Add(Task.Run(async () =>
                                {
                                    try { return await httpClient.GetStringAsync(pageUrl); }
                                    catch { return null; }
                                }));
                            }

                            var pageResults = await Task.WhenAll(pageTasks);
                            foreach (var jsonStr in pageResults)
                            {
                                if (!string.IsNullOrEmpty(jsonStr))
                                {
                                    try
                                    {
                                        var nextRecords = JsonSerializer.Deserialize<List<GameBananaRecord>>(jsonStr);
                                        if (nextRecords != null)
                                        {
                                            allMatching.AddRange(nextRecords.Where(r => MatchesCategory(r, category, subcategory)));
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }

                        lock (searchFilteredCache)
                        {
                            searchFilteredCache[searchKey] = (allMatching, DateTime.UtcNow);
                        }
                        var pagedList = allMatching.Skip((page - 1) * perPage).Take(perPage).ToList();
                        CurrentFeed.Records = new ObservableCollection<GameBananaRecord>(pagedList);
                        CurrentFeed.TotalPages = Math.Max(1, Math.Ceiling(allMatching.Count / (double)perPage));
                    }
                    catch (Exception e)
                    {
                        error = true;
                        exception = e;
                    }
                }
                return;
            }

            var requestUrl = GenerateUrl(page, game, type, filter, category, subcategory, perPage, nsfw, search, zs, colorz);
            lock (feedLock)
            {
                if (feed.ContainsKey(requestUrl) && feed[requestUrl].IsValid)
                {
                    CurrentFeed = feed[requestUrl];
                    TriggerAdjacentPrefetch(page, game, type, filter, category, subcategory, perPage, nsfw, search, zs, colorz, CurrentFeed.TotalPages);
                    return;
                }
            }

            CurrentFeed = new();
            HttpResponseMessage response = null;
            string responseString = null;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    response = await _httpClient.GetAsync(requestUrl);
                    responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if ((int)response.StatusCode == 429 && attempt == 0)
                        {
                            await Task.Delay(800);
                            continue;
                        }
                        throw new HttpRequestException($"GameBanana HTTP {(int)response.StatusCode}");
                    }

                    if (string.IsNullOrWhiteSpace(responseString))
                    {
                        CurrentFeed.Records = new ObservableCollection<GameBananaRecord>();
                        CurrentFeed.TotalPages = 1;
                        break;
                    }

                    string trimmed = responseString.TrimStart();
                    if (!trimmed.StartsWith("["))
                    {
                        if (attempt == 0)
                        {
                            await Task.Delay(600);
                            continue;
                        }
                        if (trimmed.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("too many requests", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new Exception("GameBanana rate limit reached. Please wait a few moments and click Retry.");
                        }
                        throw new Exception("GameBanana returned a temporary error or invalid response. Please click Retry.");
                    }

                    var records = JsonSerializer.Deserialize<ObservableCollection<GameBananaRecord>>(responseString);
                    CurrentFeed.Records = records ?? new ObservableCollection<GameBananaRecord>();

                    var numRecords = response.GetHeader("X-GbApi-Metadata_nRecordCount");
                    if (numRecords != -1)
                    {
                        var totalPages = Math.Ceiling(numRecords / Convert.ToDouble(perPage));
                        if (totalPages == 0) totalPages = 1;
                        CurrentFeed.TotalPages = totalPages;
                    }
                    else
                    {
                        if (CurrentFeed.Records.Count < perPage)
                            CurrentFeed.TotalPages = Math.Max(1, page);
                        else
                            CurrentFeed.TotalPages = Math.Max(page + 1, CurrentFeed.TotalPages);
                    }
                    break;
                }
                catch (Exception e)
                {
                    if (attempt == 1)
                    {
                        error = true;
                        exception = e;
                        return;
                    }
                    await Task.Delay(500);
                }
            }

            if (CurrentFeed.Records != null)
            {
                lock (feedLock)
                {
                    if (feed.Count > 50)
                    {
                        var oldest = feed.Aggregate((l, r) => DateTime.Compare(l.Value.TimeFetched, r.Value.TimeFetched) < 0 ? l : r).Key;
                        feed.Remove(oldest);
                    }
                    feed[requestUrl] = CurrentFeed;
                }

                TriggerAdjacentPrefetch(page, game, type, filter, category, subcategory, perPage, nsfw, search, zs, colorz, CurrentFeed.TotalPages);
            }
        }

        private static void TriggerAdjacentPrefetch(int page, GameFilter game, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage, bool nsfw, string search, bool zs, bool colorz, double totalPages)
        {
            if (page < totalPages)
            {
                _ = PrefetchPageAsync(page + 1, game, type, filter, category, subcategory, perPage, nsfw, search, zs, colorz);
            }
            if (page > 1)
            {
                _ = PrefetchPageAsync(page - 1, game, type, filter, category, subcategory, perPage, nsfw, search, zs, colorz);
            }
        }

        private static async Task PrefetchPageAsync(int targetPage, GameFilter game, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage, bool nsfw, string search, bool zs, bool colorz)
        {
            var nextUrl = GenerateUrl(targetPage, game, type, filter, category, subcategory, perPage, nsfw, search, zs, colorz);
            lock (feedLock)
            {
                if (feed.ContainsKey(nextUrl) && feed[nextUrl].IsValid)
                    return;
            }

            try
            {
                var response = await _httpClient.GetAsync(nextUrl);
                if (!response.IsSuccessStatusCode) return;

                var text = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(text) || !text.TrimStart().StartsWith("[")) return;

                var records = JsonSerializer.Deserialize<ObservableCollection<GameBananaRecord>>(text);
                if (records == null) return;

                var prefetched = new GameBananaModList { Records = records };
                var numRecords = response.GetHeader("X-GbApi-Metadata_nRecordCount");
                if (numRecords != -1)
                {
                    var totalPages = Math.Ceiling(numRecords / Convert.ToDouble(perPage));
                    if (totalPages == 0) totalPages = 1;
                    prefetched.TotalPages = totalPages;
                }

                lock (feedLock)
                {
                    if (feed.Count > 50)
                    {
                        var oldest = feed.Aggregate((l, r) => DateTime.Compare(l.Value.TimeFetched, r.Value.TimeFetched) < 0 ? l : r).Key;
                        feed.Remove(oldest);
                    }
                    feed[nextUrl] = prefetched;
                }
            }
            catch { }
        }
        private static string GenerateUrl(int page, GameFilter game, TypeFilter type, FeedFilter filter, GameBananaCategory category, GameBananaCategory subcategory, int perPage, bool nsfw, string search, bool zs, bool colorz)
        {
            // Base
            var url = "https://gamebanana.com/apiv6/";
            switch (type)
            {
                case TypeFilter.Mods:
                    url += "Mod/";
                    break;
                case TypeFilter.Sounds:
                    url += "Sound/";
                    break;
                case TypeFilter.WiPs:
                    url += "Wip/";
                    break;
            }
            // Different starting endpoint if requesting all mods instead of specific category
            bool hasCategory = (subcategory != null && subcategory.ID != null) || (category != null && category.ID != null);
            if (search != null)
                url += $"ByName?_sName=*{search}*&_idGameRow=11534&";
            else if (hasCategory)
                url += "ByCategory?";
            else
                url += "ByGame?_aGameRowIds[]=11534&";
            // Consistent args
            url += $"_csvProperties=_sName,_sModelName,_sProfileUrl,_aSubmitter,_tsDateUpdated,_tsDateAdded,_aPreviewMedia,_sText,_sDescription,_aCategory,_aRootCategory,_aGame,_nViewCount," +
                $"_nLikeCount,_nDownloadCount,_aFiles,_aModManagerIntegrations,_bIsNsfw,_aAlternateFileSources&_nPerpage={perPage}";
            if (!nsfw)
                url += "&_aArgs[]=_sbIsNsfw = false";
            // Sorting filter
            switch (filter)
            {
                case FeedFilter.Recent:
                    url += "&_sOrderBy=_tsDateUpdated,DESC";
                    break;
                case FeedFilter.Featured:
                    url += "&_aArgs[]=_sbWasFeatured = true& _sOrderBy=_tsDateAdded,DESC";
                    break;
                case FeedFilter.Popular:
                    url += "&_sOrderBy=_nDownloadCount,DESC";
                    break;
            }
            // Choose subcategory or category
            if (subcategory.ID != null)
                url += $"&_aCategoryRowIds[]={subcategory.ID}";
            else if (category.ID != null)
                url += $"&_aCategoryRowIds[]={category.ID}";
            
            // Get page number
            url += $"&_nPage={page}";
            // Make url unique for SZ exclusion filters

            return url;
        }
    }
}
