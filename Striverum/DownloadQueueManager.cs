using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Striverum.UI;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Striverum
{
    public class DownloadQueueItem
    {
        public GameBananaRecord Record { get; set; }
        public string DownloadUrl { get; set; }
        public string FileName { get; set; }
        public string FileDescription { get; set; }
        public string Game { get; set; }
    }

    public class DownloadProgressInfo
    {
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public double Percentage { get; set; }
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public string StatusText { get; set; }
        public bool IsIndeterminate { get; set; }
    }

    public class DownloadQueueManager
    {
        public static DownloadQueueManager Instance { get; } = new DownloadQueueManager();

        private readonly Queue<DownloadQueueItem> _queue = new();
        private readonly object _lock = new();
        private bool _isProcessing = false;
        private DownloadQueueItem _currentItem;
        private static readonly HttpClient _downloadClient = new(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 10
        });

        public int TotalBatchCount { get; private set; } = 0;
        public int CompletedBatchCount { get; private set; } = 0;

        public event Action<DownloadProgressInfo> ProgressChanged;
        public event Action<DownloadQueueItem> ItemCompleted;
        public event Action AllCompleted;

        public bool IsInQueueOrDownloading(GameBananaRecord record)
        {
            if (record == null) return false;
            lock (_lock)
            {
                if (_currentItem?.Record != null && RecordsMatch(_currentItem.Record, record))
                    return true;
                foreach (var item in _queue)
                {
                    if (item.Record != null && RecordsMatch(item.Record, record))
                        return true;
                }
            }
            return false;
        }

        private static bool RecordsMatch(GameBananaRecord a, GameBananaRecord b)
        {
            if (a == b) return true;
            if (a.Link != null && b.Link != null && string.Equals(a.Link.ToString().TrimEnd('/'), b.Link.ToString().TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrEmpty(a.Title) && !string.IsNullOrEmpty(b.Title) && string.Equals(a.Title, b.Title, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        public void QueueRecord(GameBananaRecord record, string game)
        {
            if (record == null) return;

            if (IsInQueueOrDownloading(record))
            {
                var result = MessageBox.Show(
                    $"\"{record.Title}\" is already in the download queue or currently downloading.\n\nDo you want to download it again?",
                    "Mod Already in Queue",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;
            }
            else if (record.IsInstalled || MainWindow.IsRecordInstalled(record))
            {
                var result = MessageBox.Show(
                    $"\"{record.Title}\" is already installed.\n\nDo you want to download and install it again?",
                    "Reinstall Mod",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;
            }

            if (record.AllFiles == null || record.AllFiles.Count == 0)
            {
                MessageBox.Show("This mod has no compatible files available for download.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (record.AllFiles.Count == 1)
            {
                var file = record.AllFiles[0];
                Enqueue(new DownloadQueueItem
                {
                    Record = record,
                    DownloadUrl = file.DownloadUrl,
                    FileName = file.FileName,
                    FileDescription = file.Description,
                    Game = game
                });
            }
            else
            {
                var fileBox = new UpdateFileBox(record.AllFiles, record.Title);
                fileBox.ShowDialog();
                if (fileBox.selectedDownloadAll)
                {
                    var itemsToEnqueue = new List<DownloadQueueItem>();
                    foreach (var file in record.AllFiles)
                    {
                        if (!string.IsNullOrEmpty(file.DownloadUrl) && !string.IsNullOrEmpty(file.FileName))
                        {
                            itemsToEnqueue.Add(new DownloadQueueItem
                            {
                                Record = record,
                                DownloadUrl = file.DownloadUrl,
                                FileName = file.FileName,
                                FileDescription = file.Description,
                                Game = game
                            });
                        }
                    }
                    if (itemsToEnqueue.Count > 0)
                    {
                        EnqueueRange(itemsToEnqueue);
                    }
                }
                else if (!string.IsNullOrEmpty(fileBox.chosenFileUrl) && !string.IsNullOrEmpty(fileBox.chosenFileName))
                {
                    Enqueue(new DownloadQueueItem
                    {
                        Record = record,
                        DownloadUrl = fileBox.chosenFileUrl,
                        FileName = fileBox.chosenFileName,
                        FileDescription = fileBox.chosenFileDescription,
                        Game = game
                    });
                }
            }
        }

        public void Enqueue(DownloadQueueItem item)
        {
            if (item != null)
                EnqueueRange(new[] { item });
        }

        public void EnqueueRange(IEnumerable<DownloadQueueItem> items)
        {
            if (items == null) return;
            var validItems = items.Where(i => i != null && !string.IsNullOrEmpty(i.DownloadUrl)).ToList();
            if (validItems.Count == 0) return;

            bool startProcessing = false;
            int currentIdx = 0;
            int total = 0;

            lock (_lock)
            {
                foreach (var item in validItems)
                {
                    _queue.Enqueue(item);
                }
                TotalBatchCount += validItems.Count;
                currentIdx = CompletedBatchCount + 1;
                total = TotalBatchCount;

                if (!_isProcessing)
                {
                    _isProcessing = true;
                    startProcessing = true;
                }
            }

            ReportProgress(new DownloadProgressInfo
            {
                CurrentIndex = currentIdx,
                TotalCount = total,
                Percentage = 0,
                StatusText = "Queued...",
                IsIndeterminate = false
            });

            if (startProcessing)
            {
                _ = ProcessQueueAsync();
            }
        }

        private async Task ProcessQueueAsync()
        {
            while (true)
            {
                DownloadQueueItem currentItem = null;
                int currentIdx = 0;
                int totalCount = 0;

                lock (_lock)
                {
                    if (_queue.Count == 0)
                    {
                        _currentItem = null;
                        _isProcessing = false;
                        TotalBatchCount = 0;
                        CompletedBatchCount = 0;
                        break;
                    }

                    currentItem = _queue.Dequeue();
                    _currentItem = currentItem;
                    currentIdx = CompletedBatchCount + 1;
                    totalCount = TotalBatchCount;
                }

                ReportProgress(new DownloadProgressInfo
                {
                    CurrentIndex = currentIdx,
                    TotalCount = totalCount,
                    Percentage = 0,
                    DownloadedBytes = 0,
                    TotalBytes = 0,
                    StatusText = "Connecting...",
                    IsIndeterminate = false
                });

                bool success = false;
                try
                {
                    success = await DownloadAndExtractItemAsync(currentItem, currentIdx, totalCount);
                }
                catch (Exception ex)
                {
                    Global.logger.WriteLine($"Error downloading {currentItem.FileName}: {ex.Message}", LoggerType.Error);
                }

                lock (_lock)
                {
                    CompletedBatchCount++;
                }

                if (success && currentItem.Record != null)
                {
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        currentItem.Record.IsInstalled = true;
                    });
                }

                ItemCompleted?.Invoke(currentItem);
            }

            AllCompleted?.Invoke();
        }

        private async Task<bool> DownloadAndExtractItemAsync(DownloadQueueItem item, int currentIdx, int totalCount)
        {
            string downloadsDir = Path.Combine(Global.assemblyLocation, "Downloads");
            Directory.CreateDirectory(downloadsDir);
            string destinationFilePath = Path.Combine(downloadsDir, item.FileName);

            if (File.Exists(destinationFilePath))
            {
                try { File.Delete(destinationFilePath); } catch { }
            }

            var progressReporter = new Progress<DownloadProgress>(p =>
            {
                ReportProgress(new DownloadProgressInfo
                {
                    CurrentIndex = currentIdx,
                    TotalCount = totalCount,
                    Percentage = p.Percentage * 100,
                    DownloadedBytes = p.DownloadedBytes,
                    TotalBytes = p.TotalBytes,
                    StatusText = $"{StringConverters.FormatSize(p.DownloadedBytes)} / {StringConverters.FormatSize(p.TotalBytes)}",
                    IsIndeterminate = false
                });
            });

            using (var fs = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await _downloadClient.DownloadAsync(item.DownloadUrl, fs, item.FileName, progressReporter, CancellationToken.None);
            }

            ReportProgress(new DownloadProgressInfo
            {
                CurrentIndex = currentIdx,
                TotalCount = totalCount,
                Percentage = 100,
                DownloadedBytes = 0,
                TotalBytes = 0,
                StatusText = "Installing...",
                IsIndeterminate = true
            });

            await ExtractDownloadedFileAsync(destinationFilePath, item);
            return true;
        }

        private async Task ExtractDownloadedFileAsync(string archiveFilePath, DownloadQueueItem item)
        {
            await Task.Run(() =>
            {
                string modDir = Global.GetCurrentModDirectory();
                string cleanTitle = string.Concat(item.Record.Title.Split(Path.GetInvalidFileNameChars())).Trim();
                string rawFileName = item.FileName ?? "";
                string fileTitle = !string.IsNullOrEmpty(rawFileName)
                    ? Path.GetFileNameWithoutExtension(rawFileName)
                    : cleanTitle;
                string cleanFileTitle = string.Concat(fileTitle.Split(Path.GetInvalidFileNameChars())).Trim();

                bool isMultiFile = (item.Record.AllFiles != null && item.Record.AllFiles.Count > 1) ||
                                   (!string.IsNullOrEmpty(cleanFileTitle) && !string.Equals(cleanTitle, cleanFileTitle, StringComparison.OrdinalIgnoreCase));

                string folderName = isMultiFile ? $"{cleanTitle} - {cleanFileTitle}" : cleanTitle;
                string targetDir = Path.Combine(modDir, folderName);

                int counter = 2;
                while (Directory.Exists(targetDir))
                {
                    targetDir = Path.Combine(modDir, $"{folderName} ({counter})");
                    counter++;
                }

                try
                {
                    Directory.CreateDirectory(targetDir);
                    if (Path.GetExtension(archiveFilePath).Equals(".7z", StringComparison.InvariantCultureIgnoreCase))
                    {
                        using (var archive = SevenZipArchive.Open(archiveFilePath))
                        {
                            var reader = archive.ExtractAllEntries();
                            while (reader.MoveToNextEntry())
                            {
                                if (!reader.Entry.IsDirectory)
                                    reader.WriteEntryToDirectory(targetDir, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                            }
                        }
                    }
                    else
                    {
                        using (var stream = File.OpenRead(archiveFilePath))
                        using (var reader = ReaderFactory.Open(stream))
                        {
                            while (reader.MoveToNextEntry())
                            {
                                if (!reader.Entry.IsDirectory)
                                    reader.WriteEntryToDirectory(targetDir, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                            }
                        }
                    }

                    // Create mod.json
                    string modJsonPath = Path.Combine(targetDir, "mod.json");
                    if (!File.Exists(modJsonPath))
                    {
                        var metadata = new Metadata
                        {
                            name = Path.GetFileName(targetDir),
                            group = cleanTitle,
                            filetitle = cleanFileTitle,
                            submitter = item.Record.Owner?.Name,
                            description = item.Record.Description,
                            filedescription = item.FileDescription,
                            preview = item.Record.Image,
                            homepage = item.Record.Link,
                            avi = item.Record.Owner?.Avatar,
                            upic = item.Record.Owner?.Upic,
                            cat = item.Record.RootCategory != null ? item.Record.RootCategory.Name : item.Record.Category?.Name,
                            subcategory = item.Record.Category?.Name ?? "",
                            tags = new List<string>(),
                            caticon = item.Record.Category?.Icon,
                            lastupdate = item.Record.DateUpdated
                        };
                        if (item.Record.RootCategory != null && !string.IsNullOrEmpty(item.Record.RootCategory.Name)) metadata.tags.Add(item.Record.RootCategory.Name);
                        if (item.Record.Category != null && !string.IsNullOrEmpty(item.Record.Category.Name)) metadata.tags.Add(item.Record.Category.Name);

                        if (item.Record.Category != null && item.Record.Category.HasIcon)
                        {
                            try
                            {
                                string iconCacheDir = Path.Combine(Global.assemblyLocation, "Cache", "Icons");
                                Directory.CreateDirectory(iconCacheDir);
                                string iconFileName = Path.GetFileName(item.Record.Category.Icon.LocalPath);
                                string cachedIconPath = Path.Combine(iconCacheDir, iconFileName);
                                if (!File.Exists(cachedIconPath))
                                {
                                    using (var iconClient = new HttpClient())
                                    {
                                        var bytes = iconClient.GetByteArrayAsync(item.Record.Category.Icon).Result;
                                        File.WriteAllBytes(cachedIconPath, bytes);
                                    }
                                }
                            }
                            catch { }
                        }

                        string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(modJsonPath, json);
                    }
                    else
                    {
                        try
                        {
                            var metadata = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(modJsonPath));
                            if (metadata != null)
                            {
                                metadata.group = cleanTitle;
                                metadata.filetitle = cleanFileTitle;
                                if (!string.IsNullOrEmpty(item.FileDescription))
                                    metadata.filedescription = item.FileDescription;
                                File.WriteAllText(modJsonPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
                            }
                        }
                        catch { }
                    }

                    try { File.Delete(archiveFilePath); } catch { }
                }
                catch (Exception e)
                {
                    try
                    {
                        if (Directory.Exists(targetDir) && Directory.GetFileSystemEntries(targetDir).Length == 0)
                            Directory.Delete(targetDir, true);
                    }
                    catch { }
                    MessageBox.Show($"Couldn't extract {item.FileName}: {e.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
        }

        private void ReportProgress(DownloadProgressInfo info)
        {
            ProgressChanged?.Invoke(info);
        }
    }
}
