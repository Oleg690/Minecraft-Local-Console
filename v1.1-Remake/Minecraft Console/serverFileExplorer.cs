using System.IO;
using System.Windows;

namespace Minecraft_Console
{
    class ServerFileExplorer
    {
        public static List<List<string>> GetFoldersAndFiles(string? folderPath)
        {
            List<List<string>> allItems = [];

            if (folderPath == null || !Directory.Exists(folderPath))
            {
                Console.WriteLine($"The folder '{folderPath}' does not exist.");
                return allItems; // Return an empty list
            }

            try
            {
                // Get all folders in the root folder
                string[] folders = Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly);

                foreach (var folder in folders)
                {
                    string folderName = Path.GetFileName(folder); // Get only the folder name
                    allItems.Add([folderName, "folder", folder]);
                }

                // Get all files in the root folder
                string[] files = Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file); // Get only the file name

                    // Get file info for size and last accessed time
                    FileInfo fileInfo = new(file);
                    string size = FormatFileSize(fileInfo.Length);
                    string last_open = FormatLastOpened(fileInfo.LastAccessTime);

                    // Debugging line
                    //CodeLogger.ConsoleLog($"{fileName}: {size}, {last_open}"); 

                    allItems.Add([fileName, "file", file, size, last_open]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return allItems;
        }

        public static List<string[][]> GetStructuredPathComponents(string fullPath)
        {
            var result = new List<string[][]>();
            var split = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            bool foundWorlds = false;
            string currentPath = "";

            for (int i = 0; i < split.Length; i++)
            {
                var part = split[i];

                if (string.IsNullOrWhiteSpace(part))
                    continue;

                if (!foundWorlds)
                {
                    if (part.Equals("worlds", StringComparison.OrdinalIgnoreCase))
                    {
                        foundWorlds = true;
                    }
                }

                if (foundWorlds)
                {
                    currentPath = Path.Combine(currentPath, part);

                    // Skip adding the "worlds" component itself
                    if (!part.Equals("worlds", StringComparison.OrdinalIgnoreCase))
                    {
                        var entry = new string[][]
                        {
                    [currentPath + Path.DirectorySeparatorChar],
                    [part]
                        };
                        result.Add(entry);
                    }
                }
                else
                {
                    currentPath = Path.Combine(currentPath, part);
                }
            }

            return result;
        }

        public static string? ReadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); // Try ReadWrite in case the server uses it
                using StreamReader sr = new(fs);
                return sr.ReadToEnd();
            }
            catch (IOException ex)
            {
                CodeLogger.ConsoleLog($"Failed to read file '{path}'. Error: {ex.Message}");
                MessageBox.Show($"File '{path}' is likely being actively used and could not be read.", "File In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file '{path}':\n{ex.Message}", "Read Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        public static void WriteToFile(string path, string content)
        {
            try
            {
                if (File.Exists(path))
                {
                    string existingContent = File.ReadAllText(path);

                    if (existingContent != content)
                    {
                        File.WriteAllText(path, content);
                    }
                }
                else
                {
                    File.WriteAllText(path, content); // If file does not exist, create it.
                }
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error writing to file: {ex.Message}");
                MessageBox.Show($"Unable to write to file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Help funcs for file info
        private static string FormatFileSize(long bytes)
        {
            const int KB = 1024;
            const int MB = KB * 1024;
            const int GB = MB * 1024;

            if (bytes >= GB)
                return $"{bytes / (double)GB:0.##} GB";
            else if (bytes >= MB)
                return $"{bytes / (double)MB:0.##} MB";
            else if (bytes >= KB)
                return $"{bytes / (double)KB:0.##} KB";
            else
                return bytes == 1 ? "1 Byte" : $"{bytes} Bytes";
        }

        private static string FormatLastOpened(DateTime lastOpened)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeDiff = now - lastOpened;

            if (timeDiff.TotalDays >= 2 || lastOpened.Date < now.Date.AddDays(-1))
            {
                // More than 2 days or not yesterday - show full date
                string daySuffix = GetDaySuffix(lastOpened.Day);
                return lastOpened.ToString($"MMM d'{daySuffix},' yyyy h:mmtt");
            }
            else if (lastOpened.Date == now.Date.AddDays(-1))
            {
                // Yesterday
                return "Yesterday";
            }
            else if (timeDiff.TotalHours >= 1)
            {
                // Within last 24 hours but more than 1 hour
                int hours = (int)timeDiff.TotalHours;
                return $"{hours} hour{(hours > 1 ? "s" : "")} ago";
            }
            else if (timeDiff.TotalMinutes >= 1)
            {
                // Within last hour but more than 1 minute
                int minutes = (int)timeDiff.TotalMinutes;
                return $"{minutes} minute{(minutes > 1 ? "s" : "")} ago";
            }
            else
            {
                return $"less than a minute ago";
            }
        }

        private static string GetDaySuffix(int day)
        {
            if (day >= 11 && day <= 13)
                return "th";

            return (day % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
            };
        }
    }
}