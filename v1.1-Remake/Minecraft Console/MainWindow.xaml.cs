using CreateServerFunc;
using databaseChanger;
using FileExplorer;
using Logger;
using MinecraftServerStats;
using NetworkConfig;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace Minecraft_Console
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private static readonly ServerInfoViewModel _viewModel = new();
        public static CancellationTokenSource cancellationTokenSource = new();

        private static readonly string? currentDirectory = Directory.GetCurrentDirectory();

        private static string? Server_PublicComputerIP;
        private static string? Server_LocalComputerIP;

        public static Thread? ServerStatsThread = null;

        public static bool serverRunning = false;
        public static int loadingScreenProcentage = 0;

        private static string? rootFolder;
        private static string? rootWorldsFolder;
        private static string? serverVersionsPath;
        private static string? tempFolderPath;
        private static string? defaultServerPropertiesPath;
        private static string? CurrentPath;

        private static string? serverDirectoryPath;
        private static string? selectedServer = "";
        private static string? openWorldNumber;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            CodeLogger.CreateLogFile();
            SetStaticPaths();
            LoadServersPage();
            _ = CheckRunningServersAsync();
        }

        private void NavigateToServers(object sender, RoutedEventArgs e) => LoadServersPage();
        private void NavigateToSettings(object sender, RoutedEventArgs e) => LoadPage("Settings Page");
        private void NavigateToAccount(object sender, RoutedEventArgs e) => LoadPage("Account Page");
        private void NavigateToSupport(object sender, RoutedEventArgs e) => LoadPage("Support Page");

        // Paths setter func
        private static async void SetStaticPaths()
        {
            rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory))) ?? string.Empty;
            rootWorldsFolder = Path.Combine(rootFolder, "worlds") ?? string.Empty;
            serverVersionsPath = Path.Combine(rootFolder, "versions") ?? string.Empty;
            tempFolderPath = Path.Combine(rootFolder, "temp") ?? string.Empty;
            defaultServerPropertiesPath = Path.Combine(rootFolder, "Preset Files\\server.properties") ?? string.Empty;
            Server_PublicComputerIP = await NetworkSetup.GetPublicIP() ?? string.Empty;
            Server_LocalComputerIP = NetworkSetup.GetLocalIP() ?? string.Empty;
        }

        // GIF Loader/Unloader
        private void LoadGIF()
        {
            if (rootFolder == null)
            {
                MessageBox.Show("Root folder is not set.");
                return;
            }

            var image = new BitmapImage(new Uri(Path.Combine(rootFolder, "assets\\loadingAnimations\\mining0.gif")));
            ImageBehavior.SetAnimatedSource(gifImage, image);
        }
        private void UnloadGIF()
        {
            // Remove the animated source
            ImageBehavior.SetAnimatedSource(gifImage, null);

            // Optionally force garbage collection to release the image
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // File Explorer funcs
        private void LoadFiles(string path)
        {
            if (path[^1] == '\\') path = path[..^1]; 

            if (CurrentPath == path) return;

            CurrentPath = path;
            List<string[]> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(CurrentPath);
            AddToListView(Files_Folders);

            DisplayPathComponents(pathContainer, CurrentPath);
        }

        public void BackButton_Click(object sender, RoutedEventArgs e)
        {
            string? TEMP_CurrentPath = Path.GetDirectoryName(CurrentPath);
            if (TEMP_CurrentPath == null)
            {
                MessageBox.Show("Current path is null.");
                return;
            }
            string[] pathParts = TEMP_CurrentPath.Split("\\");

            if (pathParts[^1] != "worlds")
            {
                CurrentPath = TEMP_CurrentPath;

                List<string[]> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(CurrentPath);
                AddToListView(Files_Folders);

                DisplayPathComponents(pathContainer, CurrentPath);
            }
        }

        // File Explorer Grid Creator
        public static Grid CreateGrid(int id, string Name, string Type)
        {
            if (string.IsNullOrEmpty(rootFolder))
            {
                MessageBox.Show("Root folder is not set.");
                return new Grid(); // Return an empty grid
            }

            // Create Grid
            Grid grid = new()
            {
                Name = $"Element_{id}",
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.Transparent
            };

            // Define Columns
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Create CheckBox
            CheckBox checkBox = new()
            {
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Width = 16,
                Height = 16,
                LayoutTransform = new ScaleTransform(1.5, 1.5),
                RenderTransformOrigin = new Point(0.5, 0.5),
                Background = Brushes.White,
                BorderBrush = Brushes.Transparent,
            };

            Grid.SetColumn(checkBox, 0);
            grid.Children.Add(checkBox);

            // Create Image
            Image image = new()
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(5, 0, 5, 0),
                Source = new BitmapImage(new Uri(Path.Combine(rootFolder, $"assets\\icons\\folder_file\\{Type}.png")))
            };
            Grid.SetColumn(image, 1);
            grid.Children.Add(image);

            // Create Button (Library)
            TextBlock itemOpenButton = new()
            {
                Padding = new Thickness(5, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
                Text = Name,
                FontSize = 25,
                Foreground = Brushes.White
            };
            Grid.SetColumn(itemOpenButton, 2);
            grid.Children.Add(itemOpenButton);

            // Create Button (...)
            Button optionsButton = new()
            {
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(5, 0, 5, 0),
                Width = 40,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                BorderThickness = new Thickness(2),
                BorderBrush = Brushes.White,
            };

            // Create a Viewbox to contain the content
            Viewbox viewBox = new()
            {
                Stretch = Stretch.Uniform, // Maintain aspect ratio
                StretchDirection = StretchDirection.DownOnly, // Only scale down
                VerticalAlignment = VerticalAlignment.Center, // Center vertically
                HorizontalAlignment = HorizontalAlignment.Center, // Center horizontally

                Child = new TextBlock
                {
                    Text = "...",
                    FontSize = 25,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            };

            optionsButton.Content = viewBox; // Set the Viewbox as the button's content

            Grid.SetColumn(optionsButton, 3);
            grid.Children.Add(optionsButton);

            return grid;
        }

        private void AddToListView(List<string[]> Files_Folders)
        {
            Files_Folders_ListView.Items.Clear();

            if (Files_Folders.Count > 0)
            {
                for (int i = 0; i < Files_Folders.Count; i++)
                {
                    string fileType = Files_Folders[i][0];
                    string fileName = Files_Folders[i][1];

                    ListViewItem item = new()
                    {
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Margin = new Thickness(0, 0, 0, 5),
                    };

                    Border border = new()
                    {
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(10, 10, 10, 10),
                        Child = CreateGrid(i + 1, fileName, fileType),
                        Background = new BrushConverter().ConvertFromString("#262A32") as SolidColorBrush,
                    };

                    item.Content = border;

                    if (fileType == "file")
                    {
                        item.MouseDoubleClick += (sender, e) => OpenFile(fileName);
                    }
                    else if (fileType == "folder")
                    {
                        item.MouseDoubleClick += (sender, e) => OpenFolder(fileName);
                    }

                    Files_Folders_ListView.Items.Add(item);
                }
            }
        }

        public void DisplayPathComponents(StackPanel panel, string fullPath)
        {
            panel.Children.Clear(); // clear previous buttons
            List<string[][]> components = ServerFileExplorer.GetStructuredPathComponents(fullPath);

            for (int i = 0; i < components.Count; i++)
            {
                string fullDirPath = components[i][0][0]; // full path with trailing '\'
                string folderName = components[i][1][0];  // name of the current folder

                var btn = new Button
                {
                    Content = folderName,
                    Tag = fullDirPath,
                    Margin = new Thickness(0),
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Background = Brushes.Transparent,
                    Foreground = Brushes.Gray, // Default text color is gray
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth = 0,
                    // Remove Padding from here
                    Template = (ControlTemplate)panel.FindResource("PathButtonTemplate")
                };

                btn.Click += (s, e) =>
                {
                    string? pathToLoad = ((Button)s).Tag?.ToString();
                    if (pathToLoad != null)
                    {
                        LoadFiles(pathToLoad);
                    }
                };

                panel.Children.Add(btn);

                if (i < components.Count - 1)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = "\\",
                        FontSize = 16,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Center,
                    });
                }
            }
        }

        public static void AddTextEditorItem(ListView listView, string filePath)
        {
            // Clear ListView
            listView.Items.Clear();

            string fileContent = ServerFileExplorer.ReadFromFile(filePath);

            // Outer border styling
            Border border = new()
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222428")),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(10, 10, 10, 10),
                Padding = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Stretch,
                Height = double.NaN
            };

            Grid grid = new();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Extract file name from filePath
            string fileName = Path.GetFileName(filePath);

            // Create TextBlock for file name
            TextBlock fileNameTextBlock = new()
            {
                Text = fileName,
                FontSize = 22, // Adjust as needed
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 10, 10) // Add some margin
            };

            Button saveButton = new()
            {
                Content = "Save",
                FontSize = 14,
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromRgb(0x3a, 0x8f, 0xf1)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Transparent,
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };

            TextBox textBox = new()
            {
                Text = fileContent,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, // Enable TextBox scrollbars
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Transparent,
                FontSize = 14,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 0)
            };

            saveButton.Click += (s, e) =>
            {
                try
                {
                    ServerFileExplorer.WriteToFile(filePath, textBox.Text);
                    MessageBox.Show("Saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            // Add fileNameTextBlock to the grid
            grid.Children.Add(fileNameTextBlock);
            Grid.SetRow(fileNameTextBlock, 0);
            Grid.SetColumn(fileNameTextBlock, 0);

            // Add saveButton to the grid
            grid.Children.Add(saveButton);
            Grid.SetRow(saveButton, 0);
            Grid.SetColumn(saveButton, 1); // Place it in the second column (adjust if needed)
            Grid.SetColumnSpan(saveButton, 1); // Ensure it only spans one column

            // Add textBox to the grid
            grid.Children.Add(textBox);
            Grid.SetRow(textBox, 1);
            Grid.SetColumn(textBox, 0);
            Grid.SetColumnSpan(textBox, 2); // Make it span both columns

            // Define column definitions to arrange the elements
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Allow the file name to take remaining space
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Allow the button to take its natural size

            border.Child = grid;

            ScrollViewer scrollViewer = new()
            {
                Content = border,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, // Disable ScrollViewer scroll
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0)
            };

            ListViewItem listItem = new()
            {
                Content = scrollViewer,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            Style style = new(typeof(ListViewItem), (Style)listView.FindResource(typeof(ListViewItem)));

            style.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(FocusVisualStyleProperty, null));
            style.Setters.Add(new Setter(TemplateProperty, CreateListViewItemTemplate()));

            scrollViewer.SetValue(VerticalAlignmentProperty, VerticalAlignment.Stretch);
            scrollViewer.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch);

            listItem.Style = style;

            listItem.MaxHeight = listView.ActualHeight;

            listView.Items.Add(listItem);

            listView.VerticalContentAlignment = VerticalAlignment.Stretch;
            listView.VerticalAlignment = VerticalAlignment.Stretch;

            listView.SizeChanged += (s, e) =>
            {
                foreach (ListViewItem item in listView.Items)
                {
                    item.MaxHeight = listView.ActualHeight;
                }
            };
        }

        private static ControlTemplate CreateListViewItemTemplate()
        {
            ControlTemplate template = new(typeof(ListViewItem));
            FrameworkElementFactory borderFactory = new(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            borderFactory.SetValue(Border.BorderBrushProperty, Brushes.Transparent);

            FrameworkElementFactory contentPresenterFactory = new(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Stretch);
            contentPresenterFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch);

            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;
            return template;
        }

        // Funcs for handeling files and folders
        private void OpenFolder(string fileName)
        {
            if (CurrentPath == null)
            {
                MessageBox.Show("Current path is null.");
                return;
            }

            string combinedPath = Path.Combine(CurrentPath, fileName);
            LoadFiles(combinedPath);
        }

        private void OpenFile(string fileName)
        {
            if (CurrentPath == null)
            {
                MessageBox.Show("Current path is null.");
                return;
            }

            string combinedPath = Path.Combine(CurrentPath, fileName);

            // Array of supported extensions
            string[] supportedExtensions = {
                ".txt", ".cs", ".js", ".html", ".css", ".json", ".xml", ".log",
                ".py", ".java", ".cpp", ".c", ".h", ".php", ".rb", ".go", ".swift",
                ".ts", ".jsx", ".tsx", ".vue", ".sh", ".bat", ".ini", ".config",
                ".yaml", ".yml", ".md", ".sql", ".r", ".pl", ".dart", ".kt",
                ".asm", ".pas", ".vb", ".lua", ".groovy", ".fs", ".rs", ".scala",
                ".diff", ".patch", ".cmake", ".dockerfile", ".gitignore", ".env", ".properties",
                // Minecraft server files specific extensions
                ".yml", ".json", ".properties", ".conf", ".txt", ".log", ".toml"
            };

            // Get the file extension
            string fileExtension = Path.GetExtension(combinedPath).ToLower();

            // Check if the extension is supported
            if (supportedExtensions.Contains(fileExtension))
            {
                CurrentPath = combinedPath;
                AddTextEditorItem(Files_Folders_ListView, combinedPath);
                DisplayPathComponents(pathContainer, CurrentPath);
            }
            else
            {
                MessageBox.Show($"File type '{fileExtension}' is not supported.", "Unsupported File", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Check if server is running
        private static async Task CheckRunningServersAsync()
        {
            List<object[]> dataDB = dbChanger.SpecificDataFunc($"SELECT worldNumber, Process_ID FROM worlds;");

            for (int i = 0; i < dataDB.Count; i++)
            {
                string worldNumber = dataDB[i][0].ToString() ?? string.Empty;
                string worldProcess = dataDB[i][1].ToString() ?? string.Empty;

                if (dataDB[i][1] != DBNull.Value)
                {
                    if (!IsProcessRunning(Convert.ToInt32(worldProcess)))
                    {
                        dbChanger.SpecificDataFunc($"UPDATE worlds SET serverUser = NULL, serverTempPsw = NULL, Process_ID = NULL WHERE worldNumber = \"{worldNumber}\";");
                        return;
                    }

                    if (rootWorldsFolder == null || string.IsNullOrEmpty(worldNumber))
                    {
                        MessageBox.Show("Required paths are not set.");
                        return;
                    }

                    serverDirectoryPath = Path.Combine(rootWorldsFolder, worldNumber);
                    serverRunning = true;

                    List<object[]> serverData = dbChanger.SpecificDataFunc($"SELECT Server_Port, JMX_Port, RCON_Port, RMI_Port FROM worlds WHERE worldNumber = \"{worldNumber}\";");

                    int Server_Port = 0;
                    int JMX_Port = 0;
                    int RCON_Port = 0;
                    int RMI_Port = 0;

                    foreach (object[] data in serverData)
                    {
                        Server_Port = Convert.ToInt32(data[0]);
                        JMX_Port = Convert.ToInt32(data[1]);
                        RCON_Port = Convert.ToInt32(data[2]);
                        RMI_Port = Convert.ToInt32(data[3]);
                    }

                    dbChanger.SpecificDataFunc($"UPDATE worlds SET Process_ID = \"{worldProcess}\" WHERE worldNumber = \"{worldNumber}\";");


                    if (string.IsNullOrEmpty(Server_PublicComputerIP))
                    {
                        Server_PublicComputerIP = await NetworkSetup.GetPublicIP() ?? string.Empty;

                        if (string.IsNullOrEmpty(Server_PublicComputerIP))
                        {
                            MessageBox.Show("Public IP address is not set.");
                            return;
                        }
                    }

                    ServerStatsThread = new(async () =>
                    {
                        while (!cancellationTokenSource.Token.IsCancellationRequested && serverRunning && (ServerOperator.IsPortInUse(JMX_Port) || ServerOperator.IsPortInUse(RCON_Port)))
                        {
                            CodeLogger.ConsoleLog("Correct: " + serverRunning);
                            await ServerStats.GetServerInfo(_viewModel, serverDirectoryPath, worldNumber, Server_PublicComputerIP, JMX_Port, RCON_Port, Server_Port);
                        }
                    });
                    ServerStatsThread.Start();

                    break;
                }
            }
        }

        // Load Servers Page Func
        private void LoadServersPage()
        {
            SetStatsToEmpty();
            MainContent.Children.Clear();

            ScrollViewer scrollViewer = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Grid serverGrid = new()
            {
                Margin = new Thickness(10)
            };

            Thickness buttonMargin = new(10);

            // Define 3 columns
            for (int i = 0; i < 3; i++)
                serverGrid.ColumnDefinitions.Add(new ColumnDefinition());

            int row = 0, col = 0;

            // Get all worlds from the DB
            var worldsFromDb = dbChanger.SpecificDataFunc("SELECT worldNumber, name FROM worlds");

            if (Directory.Exists(rootWorldsFolder))
            {
                HashSet<string> existingDirs = [.. Directory.GetDirectories(rootWorldsFolder)
                    .Select(d => Path.GetFileName(d))];

                foreach (var worldEntry in worldsFromDb)
                {
                    if (worldEntry.Length < 2) continue;

                    string worldNumber = worldEntry[0]?.ToString() ?? "";
                    string worldName = worldEntry[1]?.ToString() ?? "Unnamed World";

                    if (string.IsNullOrWhiteSpace(worldNumber)) continue;

                    // Check if the folder exists for this world
                    if (!existingDirs.Contains(worldNumber)) continue;

                    Button serverButton = new()
                    {
                        VerticalAlignment = VerticalAlignment.Top,
                        Content = worldName,
                        Padding = new Thickness(10),
                        Background = Brushes.LightGray,
                        Margin = buttonMargin,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Height = 220,
                        MaxWidth = 500
                    };

                    serverButton.Click += (sender, e) => OpenControlPanel(worldName, worldNumber);

                    if (serverGrid.RowDefinitions.Count <= row)
                        serverGrid.RowDefinitions.Add(new RowDefinition());

                    Grid.SetRow(serverButton, row);
                    Grid.SetColumn(serverButton, col);
                    serverGrid.Children.Add(serverButton);

                    col++;
                    if (col >= 3)
                    {
                        col = 0;
                        row++;
                    }
                }
            }

            // Ensure space for the "Create" button
            if (col >= 3)
            {
                col = 0;
                row++;
            }

            if (serverGrid.RowDefinitions.Count <= row)
                serverGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            Button serverCreateButton = new()
            {
                VerticalAlignment = VerticalAlignment.Top,
                Content = "Create Minecraft Server",
                Padding = new Thickness(10),
                Background = Brushes.LightGray,
                Margin = buttonMargin,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 220,
                MaxWidth = 500
            };

            serverCreateButton.Click += (sender, e) => CreateServerButton_Click();
            Grid.SetRow(serverCreateButton, row);
            Grid.SetColumn(serverCreateButton, col);
            serverGrid.Children.Add(serverCreateButton);

            scrollViewer.Content = serverGrid;
            MainContent.Children.Add(scrollViewer);
            HideAllServerInfoGrids(true);
            MainContent.Visibility = Visibility.Visible;
            CreateServerPage.Visibility = Visibility.Collapsed;
        }


        // Loading other pages (temp)
        private void LoadPage(string pageName)
        {
            SetStatsToEmpty();

            MainContent.Children.Clear();
            MainContent.Children.Add(new Label
            {
                Content = pageName,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            MainContent.Visibility = Visibility.Visible;
            HideAllServerInfoGrids(true);
            CreateServerPage.Visibility = Visibility.Collapsed;
        }

        private void OpenControlPanel(string serverName, string worldNumber)
        {
            selectedServer = serverName;
            openWorldNumber = worldNumber;
            SelectedServerLabel.Text = $"Manage Server: {serverName}";

            if (string.IsNullOrEmpty(openWorldNumber))
            {
                MessageBox.Show("No world selected.");
                return;
            }
            if (rootWorldsFolder == null)
            {
                MessageBox.Show("Root worlds folder is not set.");
                return;
            }

            serverDirectoryPath = Path.Combine(rootWorldsFolder, openWorldNumber);

            // Show control panel, hide server list
            MainContent.Visibility = Visibility.Collapsed;
            ServerDropBox.Visibility = Visibility.Visible;

            LoadFiles(serverDirectoryPath);
            SetStatsToEmpty(openWorldNumber);
            if (ServerDropBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string? selectedName = selectedItem.Content.ToString();

                if (FindName(selectedName) is Grid selectedGrid)
                {
                    selectedGrid.Visibility = Visibility.Visible;
                }
            }
        }

        // Server Handeling funcs
        private async void CreateServer_Click(object sender, RoutedEventArgs e)
        {
            string software = ((ComboBoxItem)SoftwareChangeBox.SelectedItem)?.Content.ToString() ?? "";  // Default to Vanilla
            string version = VersionsChangeBox.Text;  // e.g. 1.21.4
            string worldName = ServerNameInput.Text;  // e.g. My World
            int totalPlayers = Convert.ToInt32(SlotsInput.Text);
            string Server_LocalComputerIP = NetworkSetup.GetLocalIP();
            string Server_PublicComputerIP = await NetworkSetup.GetPublicIP();
            int Server_Port = 25565;
            int JMX_Port = 25562;
            int RMI_Port = 25563;
            int RCON_Port = 25575;
            int memoryAlocator = 5000; // in MB
            // Get memoryAlocator from settings

            //object[,] defaultWorldSettings = {
            //    { "max-players", $"{totalPlayers}" },
            //    { "gamemode", "survival" },
            //    { "difficulty", "normal" },
            //    { "white-list", "false" },
            //    { "online-mode", "false" },
            //    { "pvp", "true" },
            //    { "enable-command-block", "true" },
            //    { "allow-flight", "true" },
            //    { "spawn-animals", "true" },
            //    { "spawn-monsters", "true" },
            //    { "spawn-npcs", "true" },
            //    { "allow-nether", "true" },
            //    { "force-gamemode", "false" },
            //    { "spawn-protection", "0" }
            //};

            object[,] defaultWorldSettings = {
                { "max-players", $"{totalPlayers}" },
                { "gamemode", $"{GamemodeComboBox.SelectedItem.ToString() ?? string.Empty.ToLower()}" },
                { "difficulty", $"{DifficultyComboBox.SelectedItem.ToString() ?? string.Empty.ToLower()}" },
                { "white-list", $"{WhitelistCheckBox.IsChecked}" },
                { "online-mode", $"{CrackedCheckBox.IsChecked}" },
                { "pvp", $"{PVPCheckBox.IsChecked}" },
                { "enable-command-block", $"{CommandblocksCheckBox.IsChecked}" },
                { "allow-flight", $"{FlyCheckBox.IsChecked}" },
                { "spawn-animals", $"{AnimalsCheckBox.IsChecked}" },
                { "spawn-monsters", $"{MonsterCheckBox.IsChecked}" },
                { "spawn-npcs", $"{VillagersCheckBox.IsChecked}" },
                { "allow-nether", $"{NetherCheckBox.IsChecked}" },
                { "force-gamemode", $"{ForceGamemodeCheckBox.IsChecked}" },
                { "spawn-protection", $"{Convert.ToInt32(SpawnProtectionInput.Text)}" }
            };

            if (rootFolder == null || rootWorldsFolder == null || tempFolderPath == null || defaultServerPropertiesPath == null)
            {
                MessageBox.Show("One or more required paths are not set.");
                return;
            }

            SetLoadingBarProgress(0);
            LoadGIF();
            LoadingScreen.Visibility = Visibility.Visible;

            await Task.Run(async () =>
            {
                await ServerCreator.CreateServerFunc(rootFolder, rootWorldsFolder, tempFolderPath, defaultServerPropertiesPath, version, worldName, software, totalPlayers, defaultWorldSettings, memoryAlocator, Server_LocalComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port);
            });

            SetLoadingBarProgress(100);
            await Task.Delay(500);
            LoadingScreen.Visibility = Visibility.Collapsed;
            UnloadGIF();

            // After creating the server, go back to the server list
            LoadServersPage();
            MessageBox.Show($"Server Created!");
            serverRunning = false;
        }

        private void StartServer(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Starting server: {selectedServer}");

            if (serverRunning)
            {
                MessageBox.Show("An server is already running.");
                return;
            }

            if (rootWorldsFolder == null || openWorldNumber == null)
            {
                MessageBox.Show("Required paths are not set.");
                return;
            }

            serverDirectoryPath = Path.Combine(rootWorldsFolder, openWorldNumber);

            int memoryAlocator = 5000; // in MB

            List<object[]> serverData = dbChanger.SpecificDataFunc($"SELECT Server_Port, JMX_Port, RCON_Port, RMI_Port FROM worlds WHERE worldNumber = \"{openWorldNumber}\";");

            int Server_Port = 0;
            int JMX_Port = 0;
            int RCON_Port = 0;
            int RMI_Port = 0;

            foreach (object[] data in serverData)
            {
                Server_Port = Convert.ToInt32(data[0]);
                JMX_Port = Convert.ToInt32(data[1]);
                RCON_Port = Convert.ToInt32(data[2]);
                RMI_Port = Convert.ToInt32(data[3]);
            }

            if (string.IsNullOrEmpty(Server_PublicComputerIP))
            {
                MessageBox.Show("Public IP address is not set.");
                return;
            }

            Thread StartServerAsyncThread = new(async () => await ServerOperator.Start(openWorldNumber, serverDirectoryPath, memoryAlocator, Server_PublicComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port, noGUI: false, viewModel: _viewModel));
            StartServerAsyncThread.Start();
        }

        private void StopServer(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Stopping server: {selectedServer}");

            serverRunning = false;
            cancellationTokenSource.Cancel();
            List<object[]> serverData = dbChanger.SpecificDataFunc($"SELECT JMX_Port, RCON_Port FROM worlds WHERE worldNumber = \"{openWorldNumber}\";");

            int JMX_Port = 0;
            int RCON_Port = 0;

            foreach (object[] data in serverData)
            {
                JMX_Port = Convert.ToInt32(data[0]);
                RCON_Port = Convert.ToInt32(data[1]);
            }

            if (string.IsNullOrEmpty(Server_LocalComputerIP))
            {
                MessageBox.Show("Public IP address is not set.");
                return;
            }

            if (string.IsNullOrEmpty(openWorldNumber))
            {
                MessageBox.Show("World number is null.");
                return;
            }

            Thread StopServerAsyncThread = new(async () => await ServerOperator.Stop("stop", openWorldNumber, Server_LocalComputerIP, RCON_Port, JMX_Port, "00:00"));
            StopServerAsyncThread.Start();

            SetStatsToEmpty(openWorldNumber);
        }

        private void RestartServer(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Restarting server: {selectedServer}");

            if (serverRunning == false)
            {
                MessageBox.Show("No server is running.");
                return;
            }

            if (rootWorldsFolder == null || openWorldNumber == null)
            {
                MessageBox.Show("Required paths are not set.");
                return;
            }

            serverDirectoryPath = Path.Combine(rootWorldsFolder, openWorldNumber);

            int memoryAlocator = 5000; // in MB

            List<object[]> serverData = dbChanger.SpecificDataFunc($"SELECT Server_Port, JMX_Port, RCON_Port, RMI_Port FROM worlds WHERE worldNumber = \"{openWorldNumber}\";");

            int Server_Port = 0;
            int JMX_Port = 0;
            int RCON_Port = 0;
            int RMI_Port = 0;

            foreach (object[] data in serverData)
            {
                Server_Port = Convert.ToInt32(data[0]);
                JMX_Port = Convert.ToInt32(data[1]);
                RCON_Port = Convert.ToInt32(data[2]);
                RMI_Port = Convert.ToInt32(data[3]);
            }

            if (string.IsNullOrEmpty(Server_LocalComputerIP) || string.IsNullOrEmpty(Server_PublicComputerIP))
            {
                MessageBox.Show("Public or Local IP address is not set.");
                return;
            }

            serverRunning = false;
            cancellationTokenSource.Cancel();
            Thread StopServerAsyncThread = new(async () => await ServerOperator.Stop("stop", openWorldNumber, Server_LocalComputerIP, RCON_Port, JMX_Port, "00:00"));
            StopServerAsyncThread.Start();

            SetStatsToEmpty(openWorldNumber);

            Thread StartServerAsyncThread = new(async () => await ServerOperator.Start(openWorldNumber, serverDirectoryPath, memoryAlocator, Server_PublicComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port, noGUI: false, viewModel: _viewModel));
            StartServerAsyncThread.Start();
        }

        // Create Server Page Show func
        private void CreateServerButton_Click()
        {
            // Switch to the create server page
            MainContent.Visibility = Visibility.Collapsed;
            CreateServerPage.Visibility = Visibility.Visible;
            ServerDropBox.Visibility = Visibility.Collapsed;
        }

        // Control Panel DropBox Handler
        private void ServerControlPanelDropBoxChanged(object sender, RoutedEventArgs e)
        {
            HideAllServerInfoGrids();
            if (ServerDropBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string? selectedName = selectedItem.Content.ToString();

                // Show the selected grid
                if (FindName(selectedName) is Grid selectedGrid)
                {
                    selectedGrid.Visibility = Visibility.Visible;
                }
            }
        }

        // Send Command to Server
        private async void Send_Command(object sender, RoutedEventArgs e)
        {
            string? command = inputValue.Text;
            if (serverRunning == false)
            {
                MessageBox.Show("Server is not running.");
                return;
            }
            if (string.IsNullOrEmpty(command))
            {
                MessageBox.Show("Please enter a command.");
                return;
            }
            if (command == "stop")
            {
                MessageBox.Show("Stop the server from the button in the manager tab.");
                return;
            }
            if (string.IsNullOrEmpty(openWorldNumber))
            {
                MessageBox.Show("Stop the server from the button in the manager tab.");
                return;
            }

            try
            {
                string query = $"SELECT RCON_Port FROM worlds WHERE worldNumber = {openWorldNumber};";
                string? RCON_Port = dbChanger.SpecificDataFunc(query)[0][0].ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(RCON_Port))
                {
                    MessageBox.Show("Failed to retrieve the RCON port.");
                    return;
                }

                if (string.IsNullOrEmpty(Server_LocalComputerIP))
                {
                    MessageBox.Show("Local IP is not set.");
                    return;
                }

                await ServerOperator.InputForServer(command, openWorldNumber, Convert.ToInt32(RCON_Port), Server_LocalComputerIP);
                inputValue.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        // Loading Screen Progress Bar Setter
        public static void SetLoadingBarProgress(int percentage)
        {
            int width = (int)(400 * (percentage / 100.0));
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.progresBar.Width = width;
            });
            loadingScreenProcentage = percentage;
        }

        // Set stats to empty
        private static void SetStatsToEmpty(string? worldNumber)
        {
            if (rootWorldsFolder == null || worldNumber == null)
            {
                MessageBox.Show("Required paths are not set.");
                return;
            }

            serverDirectoryPath = Path.Combine(rootWorldsFolder, worldNumber);

            string? maxPlayers = dbChanger.SpecificDataFunc($"SELECT totalPlayers FROM worlds WHERE worldNumber = \"{worldNumber}\";")[0][0].ToString() ?? string.Empty;
            string WorldSize = ServerStats.GetFolderSize(serverDirectoryPath);
            string? ConsoleOutput = ServerStats.GetConsoleOutput(serverDirectoryPath);
            if (_viewModel != null)
            {
                _viewModel.Console = ConsoleOutput;
                _viewModel.MemoryUsage = "0%";
                _viewModel.UpTime = "00:00:00";
                _viewModel.WorldSize = WorldSize;
                _viewModel.PlayersOnline = $"0 / {maxPlayers}";
            }
        }

        private static void SetStatsToEmpty()
        {
            if (_viewModel != null)
            {
                _viewModel.Console = "";
                _viewModel.MemoryUsage = "0%";
                _viewModel.UpTime = "00:00:00";
                _viewModel.WorldSize = "0 MB";
                _viewModel.PlayersOnline = $"0 / 0";
            }
        }

        private void HideAllServerInfoGrids(bool verificator = false)
        {
            if (Manage != null) Manage.Visibility = Visibility.Collapsed;
            if (Console != null) Console.Visibility = Visibility.Collapsed;
            if (Files != null) Files.Visibility = Visibility.Collapsed;
            if (Stats != null) Stats.Visibility = Visibility.Collapsed;
            if (Settings != null) Settings.Visibility = Visibility.Collapsed;
            if (ServerDropBox != null && verificator == true) ServerDropBox.Visibility = Visibility.Collapsed;
        }

        private static bool IsProcessRunning(int pid)
        {
            try
            {
                Process process = Process.GetProcessById(pid);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                // Thrown when no process with the specified ID is running
                return false;
            }
        }
    }
}