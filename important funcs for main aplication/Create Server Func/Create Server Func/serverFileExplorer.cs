namespace FileExplorer
{
    class ServerFileExplorer
    {
        public static List<string> FileExplorer(string rootPath, string worldNumber)
        {
            while (true)
            {
                // Display the current path
                Console.WriteLine($"\n{rootPath}");
                Console.WriteLine("");

                // Get folders and files in the current directory
                List<string> items = GetFoldersAndFiles(rootPath);

                Console.Write("\nInsert the folder to go or exit using 'exit': ");
                string consoleInput = Console.ReadLine();

                // Handle empty input
                if (string.IsNullOrWhiteSpace(consoleInput))
                {
                    Console.WriteLine("You need to insert something!");
                    continue;
                }

                // Handle "exit" command
                if (consoleInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Bye!");
                    break;
                }

                // Handle "back" command
                if (consoleInput.Equals("back", StringComparison.OrdinalIgnoreCase))
                {
                    // Get the parent directory and check if it's the world number (root)
                    string parentDirectory = GetLastPartOfPath(rootPath);

                    if (parentDirectory != null && parentDirectory != worldNumber)
                    {
                        rootPath = Path.GetDirectoryName(rootPath); // Go back one directory
                    }
                    else
                    {
                        Console.WriteLine("You reached the root, can't go back more!");
                        continue;
                    }
                }

                // Check if the input is a file name (should not be processed as a folder)
                string combinedPath = Path.Combine(rootPath, consoleInput);
                if (consoleInput.Contains('.') && File.Exists(combinedPath))
                {
                    Console.WriteLine("You need to insert a folder name, not a file!");
                    continue;
                }

                // Check if the input is a valid folder
                if (Directory.Exists(combinedPath))
                {
                    rootPath = combinedPath; // Navigate to the new folder
                }
                else
                {
                    Console.WriteLine("Invalid folder name! Please try again.");
                }
            }

            return []; // Return an empty list if the loop ends
        }

        // -------------------------------- Help Functions --------------------------------
        private static List<string> GetFoldersAndFiles(string folderPath)
        {
            List<string> allItems = new List<string>();

            if (!Directory.Exists(folderPath))
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
                    Console.WriteLine(folderName);
                    allItems.Add(folderName); // Add folder name to the list
                }

                // Get all files in the root folder
                string[] files = Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file); // Get only the file name
                    Console.WriteLine(fileName);
                    allItems.Add(fileName); // Add file name to the list
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return allItems;
        }
        private static string GetLastPartOfPath(string path)
        {
            // Use Path.GetFileName to get the last part of the path (folder name or file name)
            return Path.GetFileName(path);
        }
    }
}
