using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Logger;
using CreateServerFunc;
using FileExplorer;
using MinecraftServerStats;
using NetworkConfig;
using System.Runtime.Versioning;
using Updater;
using databaseChanger;
using System.Drawing.Printing;
using java.lang;

namespace Minecraft_Console
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string? currentDirectory = Directory.GetCurrentDirectory();

        private readonly string serversFolderPath = @"D:\Minecraft-Server\v1.1-Remake\Minecraft Console\worlds\";

        private string? Server_PublicComputerIP;
        private string? Server_LocalComputerIP;

        private string? rootFolder;
        private string? rootWorldsFolder;
        private string? serverVersionsPath;
        private string? tempFolderPath;
        private string? defaultServerPropertiesPath;

        private string? serverDirectoryPath;
        private string? selectedServer = "";
        private string? openWorldNumber;

        public MainWindow()
        {
            InitializeComponent();
            CodeLogger.CreateLogFile();
            SetStaticPaths();
            LoadServersPage(); // Load the Servers page by default
        }

        private async void SetStaticPaths()
        {
            rootFolder = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(currentDirectory))) ?? throw new InvalidOperationException("Root folder path is null");
            rootWorldsFolder = System.IO.Path.Combine(rootFolder, "worlds");
            serverVersionsPath = System.IO.Path.Combine(rootFolder, "versions");
            tempFolderPath = System.IO.Path.Combine(rootFolder, "temp");
            defaultServerPropertiesPath = System.IO.Path.Combine(rootFolder, "Preset Files\\server.properties");
            Server_PublicComputerIP = await NetworkSetup.GetPublicIP();
            Server_LocalComputerIP = NetworkSetup.GetLocalIP();
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
            MainContent.Visibility = Visibility.Visible;
            ControlPanel.Visibility = Visibility.Collapsed; // Hide control panel
            CreateServerPage.Visibility = Visibility.Collapsed; // Hide create server page
        }

        private void CreateServerButton_Click()
        {
            // Switch to the create server page
            MainContent.Visibility = Visibility.Collapsed;
            CreateServerPage.Visibility = Visibility.Visible;
        }

        private async void CreateServer_Click(object sender, RoutedEventArgs e)
        {
            string software = ((ComboBoxItem)SoftwareComboBox.SelectedItem)?.Content.ToString() ?? "";  // Default to Vanilla
            string version = ServerVersionTextBox.Text;  // e.g. 1.21.4
            string worldName = WorldNameTextBox.Text;  // e.g. My World
            int totalPlayers = 20;
            string Server_LocalComputerIP = NetworkSetup.GetLocalIP();
            string Server_PublicComputerIP = await NetworkSetup.GetPublicIP();
            int Server_Port = 25565;
            int JMX_Port = 25562;
            int RMI_Port = 25563;
            int RCON_Port = 25575;
            bool Keep_World_On_Version_Change = true;
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


            System.Threading.Thread CreateServerAsyncThread = new(async () => await ServerCreator.CreateServerFunc(rootFolder, rootWorldsFolder, tempFolderPath, defaultServerPropertiesPath, version, worldName, software, totalPlayers, defaultWorldSettings, memoryAlocator, Server_LocalComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port));
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
            ControlPanel.Visibility = Visibility.Visible;
        }

        private void StartServer(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Starting server: {selectedServer}");

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

            System.Threading.Thread StartServerAsyncThread = new(async () => await ServerOperator.Start(openWorldNumber, serverDirectoryPath, memoryAlocator, Server_PublicComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port, noGUI: false));
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

            System.Threading.Thread StopServerAsyncThread = new(async () => await ServerOperator.Stop("stop", openWorldNumber, Server_LocalComputerIP, RCON_Port, JMX_Port, "00:00"));
            StopServerAsyncThread.Start();
        }

        private void RestartServer(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Restarting server: {selectedServer}");

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

            System.Threading.Thread RestartServerAsyncThread = new(async () => await ServerOperator.Restart(serverDirectoryPath, openWorldNumber, memoryAlocator, Server_LocalComputerIP, Server_PublicComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port, "00:00", noGUI: false));
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
            ControlPanel.Visibility = Visibility.Collapsed;
            CreateServerPage.Visibility = Visibility.Collapsed;
        }
    }
}