using Logger;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace FileExplorer
{
    class ServerFileExplorer
    {
        public static List<string[]> GetFoldersAndFiles(string? folderPath)
        {
            List<string[]> allItems = [];

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
                    Console.WriteLine($"Folder: {folderName}");
                    allItems.Add(["folder", folderName]);
                }

                // Get all files in the root folder
                string[] files = Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file); // Get only the file name
                    Console.WriteLine($"File: {fileName}");
                    allItems.Add(["file", fileName]);
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

        //public static string? ReadFromFile(string path)
        //{
        //    if (!File.Exists(path))
        //    {
        //        return null;
        //    }

        //    try
        //    {
        //        using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        //        using StreamReader sr = new(fs);
        //        return sr.ReadToEnd();
        //    }
        //    catch (IOException ex)
        //    {
        //        CodeLogger.ConsoleLog($"File '{path}' is likely being used by another process: {ex.Message}");
        //        MessageBox.Show($"File '{path}' is currently in use and cannot be read.", "File In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Error reading file '{path}':\n{ex.Message}", "Read Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        //        return null;
        //    }
        //}

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
    }
}