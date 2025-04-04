using CreateServerFunc;
using databaseChanger;
using FileExplorer;
using Logger;
using MinecraftServerStats;
using NetworkConfig;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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

        private void LoadFiles_Click()
        {
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

            List<string[]> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(serverDirectoryPath);
            AddToListView(Files_Folders);
        }

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
                Margin = new Thickness(5, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(checkBox, 0);
            grid.Children.Add(checkBox);

            // Create Image
            Image image = new()
            {
                Width = 16,
                Height = 16,
                Margin = new Thickness(5, 0, 5, 0),
                Source = new BitmapImage(new Uri(Path.Combine(rootFolder, $"assets\\icons\\folder_file\\{Type}.png")))
            };
            Grid.SetColumn(image, 1);
            grid.Children.Add(image);

            // Create Button (Library)
            TextBlock itemOpenButton = new()
            {
                Padding = new Thickness(5, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                Text = Name
            };
            Grid.SetColumn(itemOpenButton, 2);
            grid.Children.Add(itemOpenButton);

            // Create Button (...)
            Button optionsButton = new()
            {
                Margin = new Thickness(5, 8, 5, 8),
                Padding = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Content = "..."
            };
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
                        Content = CreateGrid(i + 1, fileName, fileType), // Add the dynamically created Grid
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Background = new BrushConverter().ConvertFromString("#262A32") as SolidColorBrush,
                        Margin = new Thickness(0, 0, 0, 5),
                    };

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

        private static void OpenFile(string fileName)
        {
            MessageBox.Show($"File: {fileName}");
        }

        private static void OpenFolder(string fileName)
        {
            MessageBox.Show($"Folder: {fileName}");
        }

        private static async Task CheckRunningServersAsync()
        {
            List<object[]> dataDB = dbChanger.SpecificDataFunc($"SELECT worldNumber, Process_ID FROM worlds;");

            for (int i = 0; i < dataDB.Count; i++)
            {
                string worldNumber = dataDB[i][0].ToString() ?? string.Empty;
                string worldProcess = dataDB[i][1].ToString() ?? string.Empty;

                if (dataDB[i][1] != DBNull.Value)
                {
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

                    if (rootWorldsFolder == null || string.IsNullOrEmpty(worldNumber))
                    {
                        MessageBox.Show("Required paths are not set.");
                        return;
                    }

                    if (string.IsNullOrEmpty(Server_PublicComputerIP))
                    {
                        Server_PublicComputerIP = await NetworkSetup.GetPublicIP() ?? string.Empty;

                        if (string.IsNullOrEmpty(Server_PublicComputerIP))
                        {
                            MessageBox.Show("Public IP address is not set.");
                            return;
                        }
                    }

                    serverDirectoryPath = Path.Combine(rootWorldsFolder, worldNumber);
                    serverRunning = true;

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

        private void LoadServersPage()
        {
            SetStatsToEmpty();

            // Clear existing content
            MainContent.Children.Clear();

            // Create a ScrollViewer for scrolling
            ScrollViewer scrollViewer = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            // Create a grid to hold server labels
            Grid serverGrid = new()
            {
                Margin = new Thickness(10)
            };

            // Define 3 columns
            for (int i = 0; i < 3; i++) serverGrid.ColumnDefinitions.Add(new ColumnDefinition());

            int row = 0, col = 0;

            if (Directory.Exists(rootWorldsFolder))
            {
                Button serverCreateButton = new()
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    Content = "Create Minecraft Server",
                    Padding = new Thickness(10),
                    Background = Brushes.LightGray,
                    Margin = new Thickness(5),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 220,
                    MaxWidth = 500
                };

                serverCreateButton.Click += (sender, e) => CreateServerButton_Click();

                string[] directories = Directory.GetDirectories(rootWorldsFolder);

                foreach (string dir in directories)
                {
                    string worldNumber = Path.GetFileName(dir);
                    var worldData = dbChanger.SpecificDataFunc($"SELECT name FROM worlds WHERE worldNumber = '{worldNumber}';");

                    if (worldData.Count > 0 && worldData[0].Length > 0 && worldData[0][0] != null)
                    {
                        string worldName = worldData[0][0].ToString() ?? "No world name found!";

                        // Create a clickable button for existing worlds
                        Button serverButton = new()
                        {
                            VerticalAlignment = VerticalAlignment.Top,
                            Content = worldName,
                            Padding = new Thickness(10),
                            Background = Brushes.LightGray,
                            Margin = new Thickness(5),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Height = 220,
                            MaxWidth = 500
                        };

                        // Attach click event to open control panel
                        serverButton.Click += (sender, e) => OpenControlPanel(worldName, worldNumber);

                        // Add rows dynamically
                        if (serverGrid.RowDefinitions.Count <= row)
                            serverGrid.RowDefinitions.Add(new RowDefinition());

                        // Place button in the grid
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

                Grid.SetRow(serverCreateButton, row);
                Grid.SetColumn(serverCreateButton, col);
                serverGrid.Children.Add(serverCreateButton);
            }

            // Add grid inside ScrollViewer
            scrollViewer.Content = serverGrid;

            // Show the server list
            MainContent.Children.Add(scrollViewer);
            HideAllServerInfoGrids(true);
            MainContent.Visibility = Visibility.Visible;
            CreateServerPage.Visibility = Visibility.Collapsed; // Hide create server page
        }

        private void CreateServerButton_Click()
        {
            // Switch to the create server page
            MainContent.Visibility = Visibility.Collapsed;
            CreateServerPage.Visibility = Visibility.Visible;
            ServerDropBox.Visibility = Visibility.Collapsed;
        }

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

            Thread CreateServerAsyncThread = new(async () => await ServerCreator.CreateServerFunc(rootFolder, rootWorldsFolder, tempFolderPath, defaultServerPropertiesPath, version, worldName, software, totalPlayers, defaultWorldSettings, memoryAlocator, Server_LocalComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port));
            CreateServerAsyncThread.Start();

            LoadGIF();
            LoadingScreen.Visibility = Visibility.Visible;
            while (loadingScreenProcentage < 100)
            {
                await Task.Delay(500);
            }
            SetLoadingBarProgress(100);
            await Task.Delay(500);
            LoadingScreen.Visibility = Visibility.Collapsed;
            UnloadGIF();

            // After creating the server, go back to the server list
            LoadServersPage();
            MessageBox.Show($"Server Created!");
        }

        private void BackToServersPage(object sender, RoutedEventArgs e)
        {
            // Go back to the servers list
            LoadServersPage();
        }

        private void OpenControlPanel(string serverName, string worldNumber)
        {
            selectedServer = serverName;
            openWorldNumber = worldNumber;
            SelectedServerLabel.Text = $"Manage Server: {serverName}";

            // Show control panel, hide server list
            MainContent.Visibility = Visibility.Collapsed;
            ServerDropBox.Visibility = Visibility.Visible;
            LoadFiles_Click();
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

        private void NavigateToServers(object sender, RoutedEventArgs e) => LoadServersPage();
        private void NavigateToSettings(object sender, RoutedEventArgs e) => LoadPage("Settings Page");
        private void NavigateToAccount(object sender, RoutedEventArgs e) => LoadPage("Account Page");
        private void NavigateToSupport(object sender, RoutedEventArgs e) => LoadPage("Support Page");

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

        private void HideAllServerInfoGrids(bool verificator = false)
        {
            if (Manage != null) Manage.Visibility = Visibility.Collapsed;
            if (Console != null) Console.Visibility = Visibility.Collapsed;
            if (Files != null) Files.Visibility = Visibility.Collapsed;
            if (Stats != null) Stats.Visibility = Visibility.Collapsed;
            if (Settings != null) Settings.Visibility = Visibility.Collapsed;
            if (ServerDropBox != null && verificator == true) ServerDropBox.Visibility = Visibility.Collapsed;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}