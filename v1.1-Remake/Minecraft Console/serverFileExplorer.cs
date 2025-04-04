using System.IO;

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
    }
}