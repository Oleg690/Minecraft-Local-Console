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
        private static readonly string FabricApiBaseUrl = "";
        private static readonly string ForgeApiBaseUrl = "";
        private static readonly string NeoForgeApiBaseUrl = "";
        private static readonly string PurpurApiBaseUrl = "https://api.purpurmc.org/v2/purpur/";
        private static readonly string QuiltApiBaseUrl = "";
        private static readonly string VanillaApiBaseUrl = "https://piston-meta.mojang.com/mc/game/version_manifest.json";

        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task CheckAndUpdatePurpurAsync(string downloadDirectory, string selectedVersion)
        {
            Console.WriteLine($"Checking Purpur versions in: {downloadDirectory}");
            Directory.CreateDirectory(downloadDirectory);

            // Get local versions and builds
            var localFiles = Directory.GetFiles(downloadDirectory, "purpur-*.jar");

            Console.WriteLine("Local files detected:");

            var localVersions = new Dictionary<string, int>();

            foreach (var file in localFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                //Console.WriteLine($"Processing local file: {fileName}");

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

            var availableVersions = await GetAvailablePurpurVersionsAsync(selectedVersion);
            if (availableVersions == null || availableVersions.Length == 0)
            {
                Console.WriteLine("No available versions found.");
                return;
            }

            Console.WriteLine($"Found {availableVersions.Length} versions.");
            foreach (var version in availableVersions)
            {
                //Console.WriteLine($"Processing version {version}...");

                // Fetch the latest build for the version
                int? latestBuild = await GetPurpurLatestBuildAsync(version);

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
                    await DownloadPurpurVersionAsync(version, downloadDirectory, latestBuild.Value);
                }
                else
                {
                    // Compare builds
                    int currentBuild = localVersions[version];
                    //Console.WriteLine($"Local build for {version}: {currentBuild}, Latest build: {latestBuild}");

                    if (currentBuild < latestBuild)
                    {
                        Console.WriteLine($"Purpur {version} is outdated. Updating...");
                        string oldFile = Path.Combine(downloadDirectory, $"purpur-{version}-{currentBuild}.jar");
                        if (File.Exists(oldFile))
                        {
                            Console.WriteLine($"Deleting old version: {oldFile}");
                            File.Delete(oldFile);
                        }
                        await DownloadPurpurVersionAsync(version, downloadDirectory, latestBuild.Value);
                    }
                    else
                    {
                        Console.WriteLine($"Purpur {version} is up to date.");
                    }
                }
            }

            Console.WriteLine("--------------------------------");
            Console.WriteLine("All Purpur versions are updated.");
            Console.WriteLine("--------------------------------");
        }

        public static async Task CheckAndUpdateVanillaAsync(string downloadDirectory, string selectedVersion)
        {
            Console.WriteLine($"Checking Vanilla Minecraft versions in: {downloadDirectory}");
            Directory.CreateDirectory(downloadDirectory);

            var availableVersions = await GetAvailableVanillaVersionsAsync(selectedVersion);

            if (availableVersions == null || availableVersions.Count == 0)
            {
                Console.WriteLine("No available versions found.");
                return;
            }

            Console.WriteLine($"Found {availableVersions.Count} versions.");

            var localFiles = Directory.GetFiles(downloadDirectory, "vanilla-*.jar");

            var localVersions = localFiles.Select(f => Path.GetFileNameWithoutExtension(f).Replace("vanilla-", "")).ToHashSet();

            foreach (var version in availableVersions.Keys)
            {
                if (!localVersions.Contains(version))
                {
                    Console.WriteLine($"Vanilla {version} is missing. Downloading...");
                    await DownloadVanillaVersionAsync(availableVersions[version], downloadDirectory, version);
                }
                else
                {
                    Console.WriteLine($"Vanilla {version} is up to date.");
                }
            }

            Console.WriteLine("---------------------------------");
            Console.WriteLine("All Vanilla versions are updated.");
            Console.WriteLine("---------------------------------");
        }

        //----------------------------------------------------------------------------------------------------------------------------

        private static async Task<Dictionary<string, string>> GetAvailableVanillaVersionsAsync(string? selectedVersion = null)
        {
            try
            {
                Console.WriteLine("Fetching available Vanilla Minecraft versions...");
                var response = await HttpClient.GetStringAsync(VanillaApiBaseUrl);
                using var jsonDoc = JsonDocument.Parse(response);

                var availableVersions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray()
                                    .Where(v => v.GetProperty("type").GetString() == "release")
                                    .ToDictionary(
                                        v => v.GetProperty("id").GetString(),
                                        v => v.GetProperty("url").GetString()
                                    );

                if (selectedVersion != null)
                {
                    availableVersions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray()
                                    .Where(v => v.GetProperty("type").GetString() == "release" && v.GetProperty("id").GetString() == $"{selectedVersion}")
                                    .ToDictionary(
                                        v => v.GetProperty("id").GetString(),
                                        v => v.GetProperty("url").GetString()
                                    );
                }



                foreach (var version in availableVersions)
                {
                    Console.WriteLine($"Detected version: {version.Key}");
                }

                return availableVersions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching versions: {ex.Message}");
                return null;
            }
        }

        private static async Task DownloadVanillaVersionAsync(string versionUrl, string downloadDirectory, string version)
        {
            try
            {
                var response = await HttpClient.GetStringAsync(versionUrl);
                using var jsonDoc = JsonDocument.Parse(response);
                string jarUrl = jsonDoc.RootElement.GetProperty("downloads").GetProperty("client").GetProperty("url").GetString();

                string savePath = Path.Combine(downloadDirectory, $"vanilla-{version}.jar");
                using var jarResponse = await HttpClient.GetStreamAsync(jarUrl);
                using var fileStream = File.Create(savePath);
                await jarResponse.CopyToAsync(fileStream);

                Console.WriteLine($"Downloaded Minecraft {version} successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download Minecraft {version}: {ex.Message}");
            }
        }

        private static async Task<string[]> GetAvailablePurpurVersionsAsync(string? selectedVersion = null)
        {
            try
            {
                //Console.WriteLine("Fetching available Purpur versions...");
                var response = await HttpClient.GetStringAsync(PurpurApiBaseUrl);
                
                using var jsonDoc = JsonDocument.Parse(response);
                var versions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray().Select(e => e.GetString()).ToArray();

                if (selectedVersion != null)
                {
                    versions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray().Where(v => v.GetString() == $"{selectedVersion}").Select(e => e.GetString()).ToArray();
                }
                Console.WriteLine($"API Response: ");
                foreach (var version in versions)
                {
                    Console.WriteLine($"Detected version: {version}");
                }

                return versions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching versions: {ex.Message}");
                return null;
            }
        }

        private static async Task<int?> GetPurpurLatestBuildAsync(string version)
        {
            try
            {
                var response = await HttpClient.GetStringAsync($"{PurpurApiBaseUrl}{version}");
                //Console.WriteLine($"API Response for {version}: {response}");

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

        private static async Task DownloadPurpurVersionAsync(string version, string downloadDirectory, int latestBuild)
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
                        //Console.WriteLine($"Successfully renamed Purpur {version} to {finalFilePath}");
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
                Console.WriteLine($"Downloaded Purpur {version} successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download Purpur {version}: {ex.Message}");
            }
        }

        // ------------------------ Main Updater Funcs ------------------------

        public static async Task UpdateAllSoftwaresVersions(string serverVersionsPath)
        {
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine("Starting versions updater for all softwares.");
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine();

            object[,] softwareTypes = {
                { "Vanilla", "Forge", "NeoForge", "Fabric", "Quilt", "Purpur" },
            };

            foreach (string software in softwareTypes)
            {
                string versionDirectory = Path.Combine(serverVersionsPath, software);
                await RunUpdaterForSoftware(versionDirectory, software);
            }

            Console.WriteLine();
            Console.WriteLine("-------------------------");
            Console.WriteLine("All versions are updated!");
            Console.WriteLine("-------------------------");
        }

        public static async Task UpdateSoftwareVersions(string serverVersionsPath, string software)
        {
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine($"Starting versions updater for {software}.");
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine();

            string versionDirectory = Path.Combine(serverVersionsPath, software);

            await RunUpdaterForSoftware(versionDirectory, software);

            Console.WriteLine();
            Console.WriteLine("-------------------------");
            Console.WriteLine("All versions are updated!");
            Console.WriteLine("-------------------------");
        }

        public static async Task UpdateSoftwareVersion(string serverVersionsPath, string software, string version)
        {
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine($"Starting versions updater for {software}.");
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine();

            string versionDirectory = Path.Combine(serverVersionsPath, software);

            await RunUpdaterForSoftware(versionDirectory, software, version);

            Console.WriteLine();
            Console.WriteLine("-------------------------");
            Console.WriteLine("All versions are updated!");
            Console.WriteLine("-------------------------");
        }

        // ------------------------ Main Updater Funcs Helpers ------------------------

        private static async Task RunUpdaterForSoftware(string versionDirectory, string software, string? version = null)
        {
            switch (software)
            {
                case "Vanilla":
                    await CheckAndUpdateVanillaAsync(versionDirectory, version);
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
                    await CheckAndUpdatePurpurAsync(versionDirectory, version);
                    break;
                default:
                    Console.WriteLine($"Unknown version type: {software}");
                    break;
            }
        }
    }
}