using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Onova;
using Onova.Services;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using Striverum.UI;
using System.Windows.Media.Imaging;

namespace Striverum
{
    public class AutoUpdater
    {
        private static ProgressBox progressBox;
        private static HttpClient client = new HttpClient();

        public static string GitHubOwner = "KirinToru";
        public static string GitHubRepo = "Striverum";

        static AutoUpdater()
        {
            if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Striverum-AutoUpdater");
            }
        }

        public static async Task<bool> CheckForStriverumUpdate(CancellationTokenSource cancellationToken)
        {
            // Get Version Number
            var localVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            try
            {
                var requestUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                using var response = await client.SendAsync(request, cancellationToken.Token);

                HttpResponseMessage activeResponse = response;
                HttpResponseMessage fallbackResponse = null;
                if (!activeResponse.IsSuccessStatusCode && GitHubRepo != "Unverum")
                {
                    var fallbackUrl = $"https://api.github.com/repos/{GitHubOwner}/Unverum/releases/latest";
                    using var fallbackRequest = new HttpRequestMessage(HttpMethod.Get, fallbackUrl);
                    fallbackResponse = await client.SendAsync(fallbackRequest, cancellationToken.Token);
                    if (fallbackResponse.IsSuccessStatusCode)
                    {
                        activeResponse = fallbackResponse;
                    }
                }

                if (!activeResponse.IsSuccessStatusCode)
                {
                    // Fork repository or release might not exist yet, silently return
                    fallbackResponse?.Dispose();
                    return false;
                }

                var jsonString = await activeResponse.Content.ReadAsStringAsync();
                fallbackResponse?.Dispose();
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                if (!root.TryGetProperty("tag_name", out var tagProp))
                    return false;

                string onlineTag = tagProp.GetString() ?? "";
                string onlineVersion = onlineTag.TrimStart('v', 'V');

                if (UpdateAvailable(onlineVersion, localVersion))
                {
                    string releaseTitle = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : onlineTag;
                    string releaseBody = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : "";

                    string downloadUrl = null;
                    string fileName = null;

                    if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var asset in assetsProp.EnumerateArray())
                        {
                            string assetName = asset.TryGetProperty("name", out var an) ? an.GetString() : "";
                            if (assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || assetName.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                            {
                                downloadUrl = asset.TryGetProperty("browser_download_url", out var bdu) ? bdu.GetString() : null;
                                fileName = assetName;
                                break;
                            }
                        }
                    }

                    var fakeUpdate = new GameBananaItemUpdate
                    {
                        Title = releaseTitle,
                        Version = onlineVersion,
                        Text = releaseBody ?? "",
                        Changes = new GameBananaItemUpdateChange[] { new GameBananaItemUpdateChange { Category = "Release", Text = releaseTitle } }
                    };

                    ChangelogBox notification = new ChangelogBox(fakeUpdate, "Striverum", $"A new version of Striverum is available (v{onlineVersion})!", null);
                    notification.ShowDialog();
                    notification.Activate();
                    if (notification.YesNo && !string.IsNullOrEmpty(downloadUrl) && !string.IsNullOrEmpty(fileName))
                    {
                        // Download the update
                        await DownloadStriverum(downloadUrl, fileName, onlineVersion, new Progress<DownloadProgress>(ReportUpdateProgress), cancellationToken);
                        // Notify that the update is about to happen
                        MessageBox.Show($"Finished downloading {fileName}!\nStriverum will now restart.", "Notification", MessageBoxButton.OK);
                        // Update Striverum
                        UpdateManager updateManager = new UpdateManager(new LocalPackageResolver($"{Global.assemblyLocation}{Global.s}Downloads{Global.s}StriverumUpdate"), new ZipExtractor());
                        if (!Version.TryParse(onlineVersion, out Version version))
                        {
                            MessageBox.Show($"Error parsing {onlineVersion}!\nCancelling update.", "Notification", MessageBoxButton.OK);
                            return false;
                        }
                        // Updates and restarts Striverum
                        await updateManager.PrepareUpdateAsync(version);
                        updateManager.LaunchUpdater(version);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Global.logger?.WriteLine($"Update check: {e.Message}", LoggerType.Info);
            }
            return false;
        }
        private static async Task DownloadStriverum(string uri, string fileName, string version, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken)
        {
            try
            {
                // Create the downloads folder if necessary
                if (!Directory.Exists(@$"{Global.assemblyLocation}{Global.s}Downloads"))
                {
                    Directory.CreateDirectory(@$"{Global.assemblyLocation}{Global.s}Downloads");
                }
                // Create the downloads folder if necessary
                if (!Directory.Exists(@$"{Global.assemblyLocation}{Global.s}Downloads{Global.s}StriverumUpdate"))
                {
                    Directory.CreateDirectory(@$"{Global.assemblyLocation}{Global.s}Downloads{Global.s}StriverumUpdate");
                }
                progressBox = new ProgressBox(cancellationToken);
                progressBox.progressBar.Value = 0;
                progressBox.progressText.Text = $"Downloading {fileName}";
                progressBox.Title = "Striverum Update Progress";
                progressBox.finished = false;
                progressBox.Show();
                progressBox.Activate();
                // Write and download the file
                using (var fs = new FileStream(
                    $@"{Global.assemblyLocation}{Global.s}Downloads{Global.s}StriverumUpdate/{fileName}", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
                }
                // Rename the file
                File.Move($@"{Global.assemblyLocation}{Global.s}Downloads{Global.s}StriverumUpdate{Global.s}{fileName}", $@"{Global.assemblyLocation}{Global.s}Downloads{Global.s}StriverumUpdate{Global.s}{version}.7z", true);
                progressBox.Close();
            }
            catch (OperationCanceledException)
            {
                // Remove the file is it will be a partially downloaded one and close up
                File.Delete(@$"{Global.assemblyLocation}{Global.s}Downloads{Global.s}StriverumUpdate{Global.s}{fileName}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error whilst downloading {fileName} {e.Message}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
            }
        }
        private static void ReportUpdateProgress(DownloadProgress progress)
        {
            if (progress.Percentage == 1)
            {
                progressBox.finished = true;
            }
            progressBox.progressBar.Value = progress.Percentage * 100;
            progressBox.taskBarItem.ProgressValue = progress.Percentage;
            progressBox.progressTitle.Text = $"Downloading {progress.FileName}...";
            progressBox.progressText.Text = $"{Math.Round(progress.Percentage * 100, 2)}% " +
                $"({StringConverters.FormatSize(progress.DownloadedBytes)} of {StringConverters.FormatSize(progress.TotalBytes)})";
        }
        private static bool UpdateAvailable(string onlineVersion, string localVersion)
        {
            if (onlineVersion is null || localVersion is null)
            {
                return false;
            }
            string[] onlineVersionParts = onlineVersion.Split('.');
            string[] localVersionParts = localVersion.Split('.');
            // Pad the version if one has more parts than another (e.g. 1.2.1 and 1.2)
            if (onlineVersionParts.Length > localVersionParts.Length)
            {
                for (int i = localVersionParts.Length; i < onlineVersionParts.Length; i++)
                {
                    localVersionParts = localVersionParts.Append("0").ToArray();
                }
            }
            else if (localVersionParts.Length > onlineVersionParts.Length)
            {
                for (int i = onlineVersionParts.Length; i < localVersionParts.Length; i++)
                {
                    onlineVersionParts = onlineVersionParts.Append("0").ToArray();
                }
            }
            // Decide whether the online version is new than local
            for (int i = 0; i < onlineVersionParts.Length; i++)
            {
                if (!int.TryParse(onlineVersionParts[i], out _))
                {
                    MessageBox.Show($"Couldn't parse {onlineVersion}");
                    return false;
                }
                if (!int.TryParse(localVersionParts[i], out _))
                {
                    MessageBox.Show($"Couldn't parse {localVersion}");
                    return false;
                }
                if (int.Parse(onlineVersionParts[i]) > int.Parse(localVersionParts[i]))
                {
                    return true;
                }
                else if (int.Parse(onlineVersionParts[i]) != int.Parse(localVersionParts[i]))
                {
                    return false;
                }
            }
            return false;
        }
    }
}
