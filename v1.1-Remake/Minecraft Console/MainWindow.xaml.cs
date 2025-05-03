using CreateServerFunc;
using databaseChanger;
using FileExplorer;
using FileExplorerCardsCreator;
using Logger;
using MinecraftServerStats;
using NetworkConfig;
using ServerCardsCreator;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Updater;
using WpfAnimatedGif;

namespace Minecraft_Console
{
    public static class ComboBoxExtensions
    {
        public static void ToggleDropDownOnClick(this ComboBox comboBox)
        {
            comboBox.PreviewMouseLeftButtonDown += (sender, e) =>
            {
                if (!IsClickInsidePopupContent(comboBox, e))
                {
                    comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                    e.Handled = true;
                }
            };
        }

        private static bool IsClickInsidePopupContent(ComboBox comboBox, MouseButtonEventArgs e)
        {
            var clickedElement = e.OriginalSource as DependencyObject;

            while (clickedElement != null)
            {
                // Allow ComboBoxItem or ScrollBar clicks
                if (clickedElement is ComboBoxItem || clickedElement is ScrollBar)
                    return true;

                // Bonus: if you have checkboxes, add:
                if (clickedElement is CheckBox)
                    return true;

                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            return false;
        }
    }

    public static class ButtonExtensions
    {
        public static readonly DependencyProperty IsSidebarSelectedProperty =
            DependencyProperty.RegisterAttached(
                "IsSidebarSelected",
                typeof(bool),
                typeof(ButtonExtensions),
                new FrameworkPropertyMetadata(false));

        public static bool GetIsSidebarSelected(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsSidebarSelectedProperty);
        }

        public static void SetIsSidebarSelected(DependencyObject obj, bool value)
        {
            obj.SetValue(IsSidebarSelectedProperty, value);
        }
    }

    public partial class MainWindow : Window
    {
        private static readonly ServerInfoViewModel _viewModel = new();
        public static CancellationTokenSource cancellationTokenSource = new();

        private Button? _selectedButton;

        private static Dictionary<Button, bool> _previousButtonStates = [];
        private static readonly string? currentDirectory = Directory.GetCurrentDirectory();

        private static string? Server_PublicComputerIP;
        private static string? Server_LocalComputerIP;

        public static Thread? ServerStatsThread = null;

        public static bool serverStatus = false; // false -> Offline; true -> Online
        public static bool serverRunning = false;
        public static int loadingScreenProcentage = 0;

        private static string? rootFolder;
        private static string? rootWorldsFolder;
        private static string? serverVersionsPath;
        private static string? tempFolderPath;
        private static string? defaultServerPropertiesPath;
        public static string? CurrentPath;

        private static string? serverDirectoryPath;
        private static string? selectedServer = "";
        private static string? openWorldNumber;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            CodeLogger.CreateLogFile();
            SetStaticPaths();
            OnLoaded();
            LoadServersPage();
            _ = CheckRunningServersAsync();
        }

        private void NavigateToServers(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(sender as Button);
            LoadServersPage();
        }
        private void NavigateToSettings(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(sender as Button);
            LoadPage("Settings Page");
        }
        private void NavigateToProfile(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(sender as Button);
            LoadPage("Profile Page");
        }
        private void NavigateToSupport(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(sender as Button);
            LoadPage("Support Page");
        }

        private void LoadPage(string pageName)
        {
            SetStatsToEmpty();

            MainContent.Children.Clear();
            MainContent.VerticalAlignment = VerticalAlignment.Stretch;
            MainContent.Children.Add(new Label
            {
                Content = pageName,
                Foreground = Brushes.White,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center
            });

            MainContent.Visibility = Visibility.Visible;
            HideAllServerInfoGrids(true);
            CreateServerPage.Visibility = Visibility.Collapsed;
        }

        private void SetSelectedButton(Button? button)
        {
            if (_selectedButton != null)
            {
                ButtonExtensions.SetIsSidebarSelected(_selectedButton, false);
            }
            _selectedButton = button;
            if (_selectedButton != null)
            {
                ButtonExtensions.SetIsSidebarSelected(_selectedButton, true);
            }
        }

        private void OnLoaded()
        {
            GamemodeComboBox.ToggleDropDownOnClick();
            DifficultyComboBox.ToggleDropDownOnClick();
            SoftwareChangeBox.ToggleDropDownOnClick();
            VersionsChangeBox.ToggleDropDownOnClick();
            ServerDropBox.ToggleDropDownOnClick();

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(sidebarStackPanel); i++)
            {
                if (VisualTreeHelper.GetChild(sidebarStackPanel, i) is Button firstButton)
                {
                    SetSelectedButton(firstButton);
                    break;
                }
            }
        }

        // Paths setter func
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

        // GIF Loader/Unloader
        private void LoadGIF()
        {
            if (rootFolder == null)
            {
                MessageBox.Show("Root folder is not set.");
                return;
            }

            var image = new BitmapImage(new Uri(System.IO.Path.Combine(rootFolder, "assets\\loadingAnimations\\mining0.gif")));
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
        public void BackButton_Click(object sender, RoutedEventArgs e)
        {
            string? TEMP_CurrentPath = System.IO.Path.GetDirectoryName(CurrentPath);
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
                FileExplorerCards.AddToListView(Files_Folders, Files_Folders_ListView, pathContainer);

                FileExplorerCards.DisplayPathComponents(CurrentPath, Files_Folders_ListView, pathContainer);
            }
        }

        // Check if server is running
        private async Task CheckRunningServersAsync()
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

                    serverDirectoryPath = System.IO.Path.Combine(rootWorldsFolder, worldNumber);
                    serverRunning = true;
                    serverStatus = true;

                    object[] serverData = dbChanger.SpecificDataFunc($"SELECT Server_Port, JMX_Port, RCON_Port, RMI_Port, version, totalPlayers, serverUser, serverTempPsw FROM worlds WHERE worldNumber = \"{worldNumber}\";")[0];

                    int Server_Port = Convert.ToInt32(serverData[0]);
                    int JMX_Port = Convert.ToInt32(serverData[1]);
                    int RCON_Port = Convert.ToInt32(serverData[2]);
                    int RMI_Port = Convert.ToInt32(serverData[3]);
                    object[] serverRunData = { serverData[4], serverData[5] };
                    object[] userData = { serverData[6], serverData[7] };

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
                            await ServerStats.GetServerInfo(_viewModel, serverDirectoryPath, worldNumber, serverRunData, userData, Server_PublicComputerIP, JMX_Port, RCON_Port, Server_Port);
                        }
                    });
                    ServerStatsThread.Start();

                    UpdateButtonStates(StartBtn, StopBtn, RestartBtn);

                    break;
                }
            }
        }

        // Load Servers Page Funcs
        private void LoadServersPage()
        {
            SetStatsToEmpty();
            MainContent.Children.Clear();

            ScrollViewer scrollViewer = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            WrapPanel serverPanel = new()
            {
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            int cornerRadius = 20;
            Thickness buttonMargin = new(25);
            var worldsFromDb = dbChanger.SpecificDataFunc("SELECT worldNumber, name, version, totalPlayers, Process_ID FROM worlds");
            var existingDirs = Directory.Exists(rootWorldsFolder)
                ? [.. Directory.GetDirectories(rootWorldsFolder).Select(System.IO.Path.GetFileName)]
                : new HashSet<string>();

            foreach (var worldEntry in worldsFromDb)
            {
                if (worldEntry.Length < 2) continue;

                string worldNumber = worldEntry[0]?.ToString() ?? "";
                string worldName = worldEntry[1]?.ToString() ?? "Unnamed World";
                string worldVersion = worldEntry[2]?.ToString() ?? "";
                string worldTotalPlayers = worldEntry[3]?.ToString() ?? "";
                string processID = worldEntry[4]?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(worldNumber) || !existingDirs.Contains(worldNumber))
                    continue;

                var button = ServerCards.CreateStyledButton(cornerRadius, worldName, worldVersion, worldTotalPlayers, processID, buttonMargin, () => OpenControlPanel(worldName, worldNumber));
                serverPanel.Children.Add(button);
            }

            var createButton = ServerCards.CreateStyledCreateButton(buttonMargin, cornerRadius, CreateServerButton_Click);
            serverPanel.Children.Add(createButton);

            scrollViewer.Content = serverPanel;
            MainContent.Children.Add(scrollViewer);

            HideAllServerInfoGrids(true);
            MainContent.Visibility = Visibility.Visible;
            CreateServerPage.Visibility = Visibility.Collapsed;

            SizeChanged += (s, e) => ServerCards.UpdateButtonSizes(serverPanel, MainContent.ActualWidth); // handle resize
            ServerCards.UpdateButtonSizes(serverPanel, MainContent.ActualWidth); // initial sizing
        }

        // Open Control Panel Funcs
        private void OpenControlPanel(string serverName, string worldNumber)
        {
            selectedServer = serverName;
            openWorldNumber = worldNumber;
            SelectedServerLabel.Text = $"Server Name: {serverName}";

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

            serverDirectoryPath = System.IO.Path.Combine(rootWorldsFolder, openWorldNumber);

            // Show control panel, hide server list
            MainContent.Visibility = Visibility.Collapsed;
            ServerDropBox.Visibility = Visibility.Visible;

            FileExplorerCards.LoadFiles(serverDirectoryPath, Files_Folders_ListView, pathContainer);
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
            try
            {
                CreateServerBTN.IsEnabled = false;

                // Gather inputs
                string software = ((ComboBoxItem)SoftwareChangeBox.SelectedItem)?.Content.ToString() ?? "";
                string version = VersionsChangeBox.Text;
                string worldName = ServerNameInput.Text;
                if (!int.TryParse(SlotsInput.Text, out int totalPlayers))
                {
                    MessageBox.Show("Invalid number of slots.");
                    return;
                }

                string localIP = NetworkSetup.GetLocalIP();
                string publicIP = await NetworkSetup.GetPublicIP();

                // Constants
                int Server_Port = 25565;
                int JMX_Port = 25562;
                int RMI_Port = 25563;
                int RCON_Port = 25575;
                int memoryAlocator = 5000;

                // Utility for checkboxes
                string GetCheckBoxValue(CheckBox cb, bool invert = false) =>
                    (invert ? !(cb.IsChecked ?? false) : cb.IsChecked ?? false).ToString().ToLowerInvariant();

                object[,] worldSettings =
                {
                    { "max-players", totalPlayers.ToString() },
                    { "gamemode", (GamemodeComboBox.SelectedItem as string ?? "").ToLowerInvariant() },
                    { "difficulty", (DifficultyComboBox.SelectedItem as string ?? "").ToLowerInvariant() },
                    { "white-list", GetCheckBoxValue(WhitelistCheckBox) },
                    { "online-mode", GetCheckBoxValue(CrackedCheckBox, invert: true) },
                    { "pvp", GetCheckBoxValue(PVPCheckBox) },
                    { "enable-command-block", GetCheckBoxValue(CommandblocksCheckBox) },
                    { "allow-flight", GetCheckBoxValue(FlyCheckBox) },
                    { "spawn-animals", GetCheckBoxValue(AnimalsCheckBox) },
                    { "spawn-monsters", GetCheckBoxValue(MonsterCheckBox) },
                    { "spawn-npcs", GetCheckBoxValue(VillagersCheckBox) },
                    { "allow-nether", GetCheckBoxValue(NetherCheckBox) },
                    { "force-gamemode", GetCheckBoxValue(ForceGamemodeCheckBox) },
                    { "spawn-protection", (SpawnProtectionInput.Text ?? "").ToLowerInvariant() }
                };

                if (rootFolder == null || rootWorldsFolder == null || tempFolderPath == null || defaultServerPropertiesPath == null)
                {
                    MessageBox.Show("One or more required paths are not set.");
                    return;
                }

                SetLoadingBarProgress(0);
                LoadGIF();
                LoadingScreen.Visibility = Visibility.Visible;

                string creationResult = await Task.Run(() =>
                    ServerCreator.CreateServerFunc(
                    rootFolder, rootWorldsFolder, tempFolderPath, defaultServerPropertiesPath,
                    version, worldName, software, totalPlayers,
                    worldSettings, memoryAlocator,
                    localIP, Server_Port, JMX_Port, RCON_Port, RMI_Port
                    )
                );

                SetLoadingBarProgress(100);
                await Task.Delay(500);
                LoadingScreen.Visibility = Visibility.Collapsed;
                UnloadGIF();

                if (string.IsNullOrWhiteSpace(creationResult) || creationResult.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"Failed to create server:\n{creationResult}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LoadServersPage();
                serverRunning = false;
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error creating the world. Error: {ex}");
                MessageBox.Show("An unexpected error occurred. Check logs for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CreateServerBTN.IsEnabled = true;
            }
        }

        private async void StartServer(object sender, RoutedEventArgs e)
        {
            UpdateButtonStates(StartBtn, StopBtn, RestartBtn, disableAll: true);

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
            if (string.IsNullOrEmpty(Server_PublicComputerIP))
            {
                MessageBox.Show("Public IP address is not set.");
                return;
            }

            Thread StartServerAsyncThread = new(async () => await ServerOperator.Start(openWorldNumber, serverDirectoryPath, memoryAlocator, Server_PublicComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port, noGUI: false, viewModel: _viewModel));
            StartServerAsyncThread.Start();

            while (serverRunning == false)
            {
                await Task.Delay(500);
            }

            UpdateButtonStates(StartBtn, StopBtn, RestartBtn, changeAfterEnable: true);
        }

        private void StopServer(object sender, RoutedEventArgs e)
        {
            UpdateButtonStates(StartBtn, StopBtn, RestartBtn);

            serverRunning = false;
            serverStatus = false;
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

            ServerStats.SetServerStatusOffline();
            SetStatsToEmpty(openWorldNumber);
        }

        private async void RestartServer(object sender, RoutedEventArgs e)
        {
            UpdateButtonStates(StartBtn, StopBtn, RestartBtn, disableAll: true);

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

            while (serverRunning == false)
            {
                await Task.Delay(500);
            }

            UpdateButtonStates(StartBtn, StopBtn, RestartBtn, changeAfterEnable: false);
        }

        // Create Server Page Show func
        private void CreateServerButton_Click()
        {
            ResetCreateServerButtons();

            // Switch to the create server page
            MainContent.Visibility = Visibility.Collapsed;
            CreateServerPage.Visibility = Visibility.Visible;
            ServerDropBox.Visibility = Visibility.Collapsed;

            List<string> supportedSoftwares = VersionsUpdater.GetSupportedSoftwares();

            SoftwareChangeBox.Items.Clear();
            foreach (string software in supportedSoftwares)
            {
                ComboBoxItem item = new()
                {
                    Content = software,
                    Tag = software
                };
                SoftwareChangeBox.Items.Add(item);
            }
            SoftwareChangeBox.SelectedIndex = 0; // Set default selection to the first item
        }

        private void ChangeSupportedVersions(object sender, SelectionChangedEventArgs e)
        {
            if (SoftwareChangeBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string? selectedName = selectedItem.Content.ToString();

                if (string.IsNullOrEmpty(selectedName))
                {
                    MessageBox.Show("Selected software is null.");
                    return;
                }

                // Get versions for the selected software
                List<string> versions = VersionsUpdater.GetSupportedVersions(selectedName);
                VersionsChangeBox.Items.Clear();
                foreach (string version in versions)
                {
                    ComboBoxItem item = new()
                    {
                        Content = version,
                        Tag = version
                    };
                    VersionsChangeBox.Items.Add(item);
                }

                VersionsChangeBox.SelectedIndex = 0; // Set default selection to the first item
            }
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

            serverDirectoryPath = System.IO.Path.Combine(rootWorldsFolder, worldNumber);

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

        private static void UpdateButtonStates(Button startButton, Button stopButton, Button restartButton, bool disableAll = false, bool changeAfterEnable = false)
        {
            if (disableAll)
            {
                // Save current states
                _previousButtonStates[startButton] = startButton.IsEnabled;
                _previousButtonStates[stopButton] = stopButton.IsEnabled;
                _previousButtonStates[restartButton] = restartButton.IsEnabled;

                // Disable all
                startButton.IsEnabled = false;
                stopButton.IsEnabled = false;
                restartButton.IsEnabled = false;
                return;
            }

            // Restore saved states if available
            if (_previousButtonStates.Count == 3)
            {
                startButton.IsEnabled = _previousButtonStates[startButton];
                stopButton.IsEnabled = _previousButtonStates[stopButton];
                restartButton.IsEnabled = _previousButtonStates[restartButton];
                _previousButtonStates.Clear(); // Optional: Clear after restoring

                if (changeAfterEnable)
                {
                    bool secondIsStartEnabled = startButton.IsEnabled;
                    startButton.IsEnabled = !secondIsStartEnabled;
                    stopButton.IsEnabled = secondIsStartEnabled;
                    restartButton.IsEnabled = secondIsStartEnabled;
                }
                return;
            }

            // Normal logic: Start controls others
            bool isStartEnabled = startButton.IsEnabled;
            startButton.IsEnabled = !isStartEnabled;
            stopButton.IsEnabled = isStartEnabled;
            restartButton.IsEnabled = isStartEnabled;
        }

        private void ResetCreateServerButtons()
        {
            ServerNameInput.Text = "";

            SlotsInput.Text = "20";
            GamemodeComboBox.SelectedIndex = 0;
            DifficultyComboBox.SelectedIndex = 2;
            WhitelistCheckBox.IsChecked = false;
            CrackedCheckBox.IsChecked = false;
            PVPCheckBox.IsChecked = true;
            CommandblocksCheckBox.IsChecked = true;
            FlyCheckBox.IsChecked = true;
            AnimalsCheckBox.IsChecked = true;
            MonsterCheckBox.IsChecked = true;
            VillagersCheckBox.IsChecked = true;
            NetherCheckBox.IsChecked = true;
            ForceGamemodeCheckBox.IsChecked = false;
            SpawnProtectionInput.Text = "0";
        }
    }
}