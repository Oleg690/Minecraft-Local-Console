using CreateServerFunc;
using databaseChanger;
using Logger;
using MinecraftServerStats;
using NetworkConfig;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Minecraft_Console
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ServerInfoViewModel _viewModel = new();

        private static readonly string? currentDirectory = Directory.GetCurrentDirectory();

        private static readonly string serversFolderPath = @"D:\Minecraft-Server\v1.1-Remake\Minecraft Console\worlds\";

        private static string? Server_PublicComputerIP;
        private static string? Server_LocalComputerIP;

        public static bool serverRunning = false;

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
            LoadServersPage(); // Load the Servers page by default
        }

        private static async void SetStaticPaths()
        {
            rootFolder = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(currentDirectory))) ?? string.Empty;
            rootWorldsFolder = System.IO.Path.Combine(rootFolder, "worlds") ?? string.Empty;
            serverVersionsPath = System.IO.Path.Combine(rootFolder, "versions") ?? string.Empty;
            tempFolderPath = System.IO.Path.Combine(rootFolder, "temp") ?? string.Empty;
            defaultServerPropertiesPath = System.IO.Path.Combine(rootFolder, "Preset Files\\server.properties") ?? string.Empty;
            Server_PublicComputerIP = await NetworkSetup.GetPublicIP() ?? string.Empty;
            Server_LocalComputerIP = NetworkSetup.GetLocalIP() ?? string.Empty;
        }

        private void LoadServersPage()
        {
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

            if (Directory.Exists(serversFolderPath))
            {
                string[] directories = Directory.GetDirectories(serversFolderPath);

                foreach (string dir in directories)
                {
                    string? worldNumber = System.IO.Path.GetFileName(dir);

                    string? worldName = dbChanger.SpecificDataFunc($"SELECT name FROM worlds where worldNumber = '{worldNumber}';")[0][0].ToString() ?? "No world name found!";

                    // Create a clickable button instead of a Label
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
            ServerControlPanelDropBox.Visibility = Visibility.Visible;
        }

        private async void CreateServer_Click(object sender, RoutedEventArgs e)
        {
            string software = ((ComboBoxItem)SoftwareComboBox.SelectedItem)?.Content.ToString() ?? "";  // Default to Vanilla
            string version = ServerVersionTextBox.Text;  // e.g. 1.21.4
            string worldName = WorldNameTextBox.Text;  // e.g. My World
            int totalPlayers = Convert.ToInt32(TotalPlayersTextBox.Text);
            string Server_LocalComputerIP = NetworkSetup.GetLocalIP();
            string Server_PublicComputerIP = await NetworkSetup.GetPublicIP();
            int Server_Port = 25565;
            int JMX_Port = 25562;
            int RMI_Port = 25563;
            int RCON_Port = 25575;
            int memoryAlocator = 5000; // in MB
            // Get memoryAlocator from settings

            object[,] defaultWorldSettings = {
                { "max-players", $"{totalPlayers}" },
                { "gamemode", "survival" },
                { "difficulty", "normal" },
                { "white-list", "false" },
                { "online-mode", "false" },
                { "pvp", "true" },
                { "enable-command-block", "true" },
                { "allow-flight", "true" },
                { "spawn-animals", "true" },
                { "spawn-monsters", "true" },
                { "spawn-npcs", "true" },
                { "allow-nether", "true" },
                { "force-gamemode", "false" },
                { "spawn-protection", "0" }
            };

            if (rootFolder == null || rootWorldsFolder == null || tempFolderPath == null || defaultServerPropertiesPath == null)
            {
                MessageBox.Show("One or more required paths are not set.");
                return;
            }

            Thread CreateServerAsyncThread = new(async () => await ServerCreator.CreateServerFunc(rootFolder, rootWorldsFolder, tempFolderPath, defaultServerPropertiesPath, version, worldName, software, totalPlayers, defaultWorldSettings, memoryAlocator, Server_LocalComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port));
            CreateServerAsyncThread.Start();

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

            if (ServerDropBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string? selectedName = selectedItem.Content.ToString();

                if (FindName(selectedName) is Grid selectedGrid)
                {
                    selectedGrid.Visibility = Visibility.Visible;
                }
            }
            ServerControlPanelDropBox.Visibility = Visibility.Visible;
        }

        private void StartServer(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Starting server: {selectedServer}");

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

            serverRunning = false;
            Thread StopServerAsyncThread = new(async () => await ServerOperator.Stop("stop", openWorldNumber, Server_LocalComputerIP, RCON_Port, JMX_Port, "00:00"));
            StopServerAsyncThread.Start();

            SetStatsToEmpty(_viewModel, openWorldNumber);
        }

        private void RestartServer(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Restarting server: {selectedServer}");

            if (rootWorldsFolder == null || openWorldNumber == null)
            {
                MessageBox.Show("Required paths are not set.");
                return;
            }

            serverDirectoryPath = System.IO.Path.Combine(rootWorldsFolder, openWorldNumber);

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

            if (string.IsNullOrEmpty(Server_PublicComputerIP) || string.IsNullOrEmpty(Server_LocalComputerIP))
            {
                MessageBox.Show("Public or Local IP address is not set.");
                return;
            }

            serverRunning = false;
            Thread RestartServerAsyncThread = new(async () => await ServerOperator.Restart(serverDirectoryPath, openWorldNumber, memoryAlocator, Server_LocalComputerIP, Server_PublicComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port, "00:00", noGUI: false));
            RestartServerAsyncThread.Start();
        }

        private void NavigateToServers(object sender, RoutedEventArgs e) => LoadServersPage();
        private void NavigateToSettings(object sender, RoutedEventArgs e) => LoadPage("Settings Page");
        private void NavigateToAccount(object sender, RoutedEventArgs e) => LoadPage("Account Page");
        private void NavigateToSupport(object sender, RoutedEventArgs e) => LoadPage("Support Page");

        private void LoadPage(string pageName)
        {
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
                    if (selectedName == "Console")
                    {
                        SetStatsToEmpty(_viewModel, openWorldNumber);
                    }

                    selectedGrid.Visibility = Visibility.Visible;
                }
            }
        }

        private static void SetStatsToEmpty(ServerInfoViewModel? viewModel, string? worldNumber)
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
            if (viewModel != null)
            {
                viewModel.Console = ConsoleOutput;
                viewModel.MemoryUsage = "0%";
                viewModel.UpTime = "00:00:00";
                viewModel.WorldSize = WorldSize;
                viewModel.PlayersOnline = $"0 / {maxPlayers}";
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
            if (ServerControlPanelDropBox != null && verificator == true) ServerControlPanelDropBox.Visibility = Visibility.Collapsed;
        }
    }
}