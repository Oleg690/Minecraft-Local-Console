using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;

namespace updater
{
    class versionsUpdater
    {
        private static readonly string PurpurApiBaseUrl = "https://api.purpurmc.org/v2/purpur/";
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task CheckAndUpdatePurpurAsync(string downloadDirectory)
        {
            Console.WriteLine($"Checking Purpur versions in: {downloadDirectory}");
            Directory.CreateDirectory(downloadDirectory);

            var availableVersions = await GetAvailableVersionsAsync();
            if (availableVersions == null || availableVersions.Length == 0)
            {
                Console.WriteLine("No available versions found.");
                return;
            }

            Console.WriteLine($"Found {availableVersions.Length} versions.");

            // Get local versions and builds
            var localFiles = Directory.GetFiles(downloadDirectory, "purpur-*.jar");

            Console.WriteLine("Local files detected:");
            foreach (var file in localFiles)
            {
                Console.WriteLine($" - {file}");
            }

            var localVersions = new Dictionary<string, int>();

            foreach (var file in localFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                Console.WriteLine($"Processing local file: {fileName}");

                var parts = fileName.Replace("purpur-", "").Split('-');
                if (parts.Length == 1) // Handle filenames without build numbers
                {
                    string version = parts[0];
                    localVersions[version] = 0; // Assume build number is 0 or handle as needed
                    Console.WriteLine($"Detected version: {version}, Build: 0 (assumed)");
                }
                else if (parts.Length == 2 && int.TryParse(parts[1], out int build)) // Handle filenames with build numbers
                {
                    localVersions[parts[0]] = build;
                    Console.WriteLine($"Detected version: {parts[0]}, Build: {build}");
                }
                else
                {
                    Console.WriteLine($"Skipping {fileName} - Unrecognized format");
                }
            }

            Console.WriteLine("Local versions detected:");
            foreach (var kvp in localVersions)
            {
                Console.WriteLine($" - Version: {kvp.Key}, Build: {kvp.Value}");
            }

            foreach (var version in availableVersions)
            {
                Console.WriteLine($"Processing version {version}...");

                // Fetch the latest build for the version
                int? latestBuild = await GetLatestBuildAsync(version);

                if (latestBuild == null)
                {
                    Console.WriteLine($"Skipping {version} because no latest build was found.");
                    continue;
                }

                // Check if the version is missing or if the local file doesn't have a build number
                if (!localVersions.ContainsKey(version) || localVersions[version] == 0)
                {
                    // Delete the old file if it exists (without a build number)
                    string oldFileWithoutBuild = Path.Combine(downloadDirectory, $"purpur-{version}.jar");
                    if (File.Exists(oldFileWithoutBuild))
                    {
                        Console.WriteLine($"Deleting old version without build number: {oldFileWithoutBuild}");
                        File.Delete(oldFileWithoutBuild);
                    }

                    // Download the latest version and rename it to include the build number
                    Console.WriteLine($"Purpur {version} is missing or outdated. Downloading...");
                    await DownloadLatestVersionAsync(version, downloadDirectory, latestBuild.Value);
                }
                else
                {
                    // Compare builds
                    int currentBuild = localVersions[version];
                    Console.WriteLine($"Local build for {version}: {currentBuild}, Latest build: {latestBuild}");

                    if (currentBuild < latestBuild)
                    {
                        Console.WriteLine($"Purpur {version} is outdated. Updating...");
                        string oldFile = Path.Combine(downloadDirectory, $"purpur-{version}-{currentBuild}.jar");
                        if (File.Exists(oldFile))
                        {
                            Console.WriteLine($"Deleting old version: {oldFile}");
                            File.Delete(oldFile);
                        }
                        await DownloadLatestVersionAsync(version, downloadDirectory, latestBuild.Value);
                    }
                    else
                    {
                        Console.WriteLine($"Purpur {version} is up to date.");
                    }
                }
            }
        }

        private static async Task<string[]> GetAvailableVersionsAsync()
        {
            try
            {
                Console.WriteLine("Fetching available Purpur versions...");
                var response = await HttpClient.GetStringAsync(PurpurApiBaseUrl);
                Console.WriteLine($"API Response: {response}");
                using var jsonDoc = JsonDocument.Parse(response);
                return jsonDoc.RootElement.GetProperty("versions").EnumerateArray().Select(e => e.GetString()).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching versions: {ex.Message}");
                return null;
            }
        }

        private static async Task<int?> GetLatestBuildAsync(string version)
        {
            try
            {
                Console.WriteLine($"Fetching latest build for {version}...");
                var response = await HttpClient.GetStringAsync($"{PurpurApiBaseUrl}{version}");
                Console.WriteLine($"API Response for {version}: {response}");

                using var jsonDoc = JsonDocument.Parse(response);

                var buildsArray = jsonDoc.RootElement.GetProperty("builds").GetProperty("all").EnumerateArray();
                var buildNumbers = buildsArray
                    .Select(e => int.TryParse(e.GetString(), out int num) ? num : (int?)null)
                    .Where(num => num.HasValue)
                    .Select(num => num.Value);

                return buildNumbers.Any() ? buildNumbers.Max() : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching latest build for {version}: {ex.Message}");
                return null;
            }
        }

        private static async Task DownloadLatestVersionAsync(string version, string downloadDirectory, int latestBuild)
        {
            try
            {
                string url = $"{PurpurApiBaseUrl}{version}/latest/download";
                string tempFilePath = Path.Combine(downloadDirectory, $"purpur-{version}.jar");
                string finalFilePath = Path.Combine(downloadDirectory, $"purpur-{version}-{latestBuild}.jar");

                using (var response = await HttpClient.GetStreamAsync(url))
                {
                    using (var fileStream = File.Create(tempFilePath))
                    {
                        await response.CopyToAsync(fileStream);
                    }
                }

                if (!File.Exists(finalFilePath))
                {
                    try
                    {
                        File.Move(tempFilePath, finalFilePath);
                        Console.WriteLine($"Successfully renamed Purpur {version} to {finalFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to rename Purpur {version}: {ex.Message}");
                        // Clean up both files if move fails
                        if (File.Exists(finalFilePath))
                            File.Delete(finalFilePath);
                        File.Delete(tempFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download Purpur {version}: {ex.Message}");
            }
        }

        public static async Task UpdateAvailableVersions(string serverVersionsPath)
        {
            Console.WriteLine("Starting versions updater for all softwares.");

            object[,] softwareTypes = {
                { "Vanilla", "Forge", "NeoForge", "Fabric", "Quilt", "Purpur" },
            };

            foreach (string software in softwareTypes)
            {
                string versionDirectory = Path.Combine(serverVersionsPath, software);
                switch (software)
                {
                    case "Vanilla":
                        break;
                    case "Forge":
                        break;
                    case "NeoForge":
                        break;
                    case "Fabric":
                        break;
                    case "Quilt":
                        break;
                    case "Purpur":
                        await CheckAndUpdatePurpurAsync(versionDirectory);
                        break;
                    default:
                        Console.WriteLine($"Unknown version type: {software}");
                        break;
                }
            }

            Console.WriteLine("All versions are updated!");
        }

        public static async Task UpdateAvailableVersions(string serverVersionsPath, string software)
        {
            Console.WriteLine($"Starting versions updater for {software}.");

            string versionDirectory = Path.Combine(serverVersionsPath, software);
            switch (software)
            {
                case "Purpur":
                    await CheckAndUpdatePurpurAsync(versionDirectory);
                    break;
                default:
                    Console.WriteLine($"Unknown version type: {software}");
                    break;
            }

            Console.WriteLine("All versions are updated!");
        }
    }
}