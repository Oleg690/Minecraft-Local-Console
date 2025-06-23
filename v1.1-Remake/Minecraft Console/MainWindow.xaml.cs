using Minecraft_Console.UI;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace Minecraft_Console
{
    public class ServerWorld
    {
        public bool IsActive { get; set; }
        public int IsRunning { get; set; }
        public Color StatusColor { get; set; }
        public bool ButtonState1 { get; set; }
        public bool ButtonState2 { get; set; }
        public bool ButtonState3 { get; set; }
    }

    public static class ComboBoxExtensions
    {
        public static void ToggleDropDownOnClick(this ComboBox comboBox)
        {
            comboBox.PreviewMouseLeftButtonDown += (sender, e) =>
            {
                if (!IsClickInsidePopupContent(e))
                {
                    comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                    e.Handled = true;
                }
            };
        }

        private static bool IsClickInsidePopupContent(MouseButtonEventArgs e)
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
        private static ViewModel? viewModel;
        private static readonly ViewModel _viewModel = new("default");
        private readonly Dictionary<string, ServerMonitor> _activeMonitors = [];
        private Button? _selectedButton;

        private static readonly Color OfflineStatusColor = Color.FromRgb(255, 53, 53);
        private static readonly Color OnlineStatusColor = Color.FromRgb(135, 255, 44);
        private static readonly Color LoadingStatusColor = Color.FromRgb(255, 165, 0);

        public Dictionary<string, ServerWorld>? ServerWorlds = [];

        public bool ExplorerPopupStatus = false; 
        private bool noGUIonServerStartup = true;

        private static readonly string? currentDirectory = Directory.GetCurrentDirectory();

        private static string? Server_PublicComputerIP;
        private static string? Server_LocalComputerIP;

        public static Thread? ServerStatsThread = null;

        public static bool processUnderWay = false; // false -> Offline; true -> Online
        public static bool serverStatus = false; // false -> Offline; true -> Online
        public static bool serverRunning = false;
        public static int loadingScreenProcentage = 0;

        private static string? rootFolder;
        public static string? rootWorldsFolder;
        private static string? serverVersionsPath;
        private static string? tempFolderPath;
        private static string? defaultServerPropertiesPath;
        public static string? appDataPath;
        public static string? CurrentPath;

        private static string? serverDirectoryPath;
        private static string? selectedServer = "";
        public static string? openWorldNumber;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        // Loading data func
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await SetStaticPaths();

            CodeLogger.CreateLogFile(5);
            LoadDataJSONFile();
            OnLoaded();
            LoadServersPage();
            StartMonitorsOnStartup();
        }

        // Closing func
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            StopAllMonitors();
        }

        // Load .json file data
        private static void LoadDataJSONFile()
        {
            if (string.IsNullOrEmpty(appDataPath))
            {
                throw new InvalidOperationException("The 'appData' path is not set.");
            }

            JsonHelper.CreateJsonIfNotExists(appDataPath, new Dictionary<string, object>
            {
                { "runningServerMemory", "5000" },
                { "archivePath", $"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")}" }
            });
        }

        // Helpers for sidebar navigation
        private void NavigateToServers(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(sender as Button);
            LoadServersPage();
            MainMenu();
        }
        private void NavigateToSettings(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(sender as Button);
            LoadPage("Settings Page");
            MainMenu();
        }
        private void NavigateToProfile(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(sender as Button);
            LoadPage("Profile Page");
            MainMenu();
        }
        private void NavigateToSupport(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(sender as Button);
            LoadPage("Support Page");
            MainMenu();
        }

        private static void MainMenu()
        {
            openWorldNumber = string.Empty;
        }

        private void LoadPage(string pageName)
        {
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
        private static async Task SetStaticPaths()
        {
            var publicIpTask = NetworkSetup.GetPublicIP();
            rootFolder = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(currentDirectory))) ?? string.Empty;
            rootWorldsFolder = System.IO.Path.Combine(rootFolder, "worlds") ?? string.Empty;
            serverVersionsPath = System.IO.Path.Combine(rootFolder, "versions") ?? string.Empty;
            tempFolderPath = System.IO.Path.Combine(rootFolder, "temp") ?? string.Empty;
            defaultServerPropertiesPath = System.IO.Path.Combine(rootFolder, "Preset Files\\server.properties") ?? string.Empty;
            appDataPath = System.IO.Path.Combine(rootFolder, "appProperties\\appData.json") ?? string.Empty;
            Server_LocalComputerIP = NetworkSetup.GetLocalIP() ?? string.Empty;
            Server_PublicComputerIP = await publicIpTask;
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
        public void ChangeAllCheckBoxes(object sender, RoutedEventArgs e)
        {
            var container = FindVisualParent<Grid>(ExplorerParent);
            if (container == null) return;

            bool shouldCheck = SelectAllCheckBox.IsChecked == true;

            foreach (var checkBox in FindVisualChildren<CheckBox>(container))
            {
                if (checkBox != SelectAllCheckBox) // avoid checking the controller itself
                {
                    checkBox.IsChecked = shouldCheck;
                }
            }
        }

        // Load Servers Page Funcs
        private void LoadServersPage()
        {
            _viewModel.IsActivePanel = false;
            MainContent.Children.Clear();
            MainContent.Opacity = 0;

            LoadExistingServerStatus();

            ScrollViewer scrollViewer = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            WrapPanel serverPanel = new()
            {
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Orientation = Orientation.Horizontal
            };

            int cornerRadius = 20;
            Thickness buttonMargin = new(25);
            var worldsData = DbChanger.SpecificDataFunc("SELECT worldNumber, name, version, totalPlayers, Server_Port, JMX_Port, Process_ID FROM worlds");
            var existingDirs = Directory.Exists(rootWorldsFolder)
                ? [.. Directory.GetDirectories(rootWorldsFolder).Select(Path.GetFileName)]
                : new HashSet<string>();

            foreach (var worldEntry in worldsData)
            {
                if (worldEntry.Length < 2 || ServerWorlds == null) continue;

                string worldNumber = worldEntry[0]?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(worldNumber))
                    continue;

                if (!existingDirs.Contains(worldNumber))
                {
                    DbChanger.SpecificDataFunc($"DELETE FROM worlds WHERE worldNumber == \"{worldNumber}\";");
                    continue;
                }

                object[] serverData = DbChanger.GetFunc(worldNumber)[0];

                string worldName = serverData[2]?.ToString() ?? "Unnamed World";
                string worldVersion = serverData[3]?.ToString() ?? "";
                string totalPlayers = serverData[5]?.ToString() ?? "";
                string processID = worldEntry[6]?.ToString() ?? "";

                int mainPort = Convert.ToInt32(serverData[6]);
                int jmxPort = Convert.ToInt32(serverData[7]);

                var button = ServerCard.CreateStyledButton(worldNumber, cornerRadius, worldName, worldVersion, totalPlayers, processID, buttonMargin, ServerWorlds[worldNumber].IsRunning, () => OpenControlPanel(worldName, worldNumber));

                serverPanel.Children.Add(button);
            }

            var createButton = ServerCard.CreateStyledCreateButton(buttonMargin, cornerRadius, CreateServerButton_Click);
            serverPanel.Children.Add(createButton);

            scrollViewer.Content = serverPanel;
            MainContent.Children.Add(scrollViewer);

            HideAllServerInfoGrids(true);
            CreateServerPage.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;

            // Handle dynamic resizing
            SizeChanged += (s, e) => ServerCard.UpdateButtonSizes(serverPanel, MainContent.ActualWidth);

            // Delay initial sizing to ensure layout is updated
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ServerCard.UpdateButtonSizes(serverPanel, MainContent.ActualWidth);
            }), DispatcherPriority.Loaded);

            ServerCard.AnimateFadeIn(MainContent);
        }

        private void StartMonitorsOnStartup()
        {
            var worldsData = DbChanger.SpecificDataFunc("SELECT worldNumber, name, version, totalPlayers, Server_Port, JMX_Port, Process_ID FROM worlds");

            foreach (var worldEntry in worldsData)
            {
                if (worldEntry.Length < 2 || ServerWorlds == null)
                    continue;

                string worldNumber = worldEntry[0]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(worldNumber))
                    continue;

                string processID = worldEntry[6]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(processID))
                    continue; // no running process → no monitor

                // Check if ServerWorlds says it's running
                if (!ServerWorlds.TryGetValue(worldNumber, out var serverWorld) || serverWorld.IsRunning != 0)
                    continue; // not running according to status → skip

                // Check if monitor is already active and running
                if (_activeMonitors.TryGetValue(worldNumber, out var monitor) && monitor.IsRunning)
                    continue; // monitor already running, skip

                // Ok, start monitor for this server
                if (string.IsNullOrWhiteSpace(rootWorldsFolder) || string.IsNullOrWhiteSpace(Server_PublicComputerIP))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Required fields are not set. Cannot start server monitoring.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    continue;
                }

                object[] serverData = DbChanger.GetFunc(worldNumber)[0];
                int mainPort = Convert.ToInt32(serverData[6]);
                int jmxPort = Convert.ToInt32(serverData[7]);
                string[] userData = [$"{serverData[10]}", $"{serverData[11]}"];

                var vm = ViewModelManager.Create(worldNumber);
                var newMonitor = new ServerMonitor(vm, worldNumber);

                string serverWorldPath = Path.Combine(rootWorldsFolder, worldNumber);

                // Start monitoring (non-blocking)
                Task.Run(() =>
                {
                    newMonitor.StartMonitoring(worldNumber, serverData, userData, serverWorldPath, Server_PublicComputerIP, jmxPort, mainPort);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _activeMonitors[worldNumber] = newMonitor;
                    });
                });
            }
        }

        private void AnimateStatusColor(Color newColor, string worldNumber, int time)
        {
            ServerWorld? world = GetActiveWorld(worldNumber);
            if (world == null)
                return;

            world.StatusColor = newColor;

            // Map colors to running states and status text
            var colorStateMap = new Dictionary<Color, (int IsRunning, string StatusText)>
            {
                { OnlineStatusColor, (0, "Online") },
                { LoadingStatusColor, (1, "Loading") },
                { OfflineStatusColor, (2, "Offline") }
            };

            if (colorStateMap.TryGetValue(newColor, out var state))
            {
                world.IsRunning = state.IsRunning;

                if (worldNumber == openWorldNumber || worldNumber == "default")
                {
                    if (FindResource("StatusBrush") is SolidColorBrush brush)
                    {
                        var colorAnimation = new ColorAnimation
                        {
                            To = newColor,
                            Duration = TimeSpan.FromMilliseconds(time),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                        };
                        brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                    }

                    statusText.Text = state.StatusText;
                }
            }

            if (string.IsNullOrEmpty(openWorldNumber) && (world.IsRunning == 0 || world.IsRunning == 2))
            {
                LoadServersPage();
            }
        }

        private void LoadExistingServerStatus()
        {
            var brush = (SolidColorBrush)FindResource("StatusBrush");

            var colorAnimation = new ColorAnimation
            {
                To = OfflineStatusColor,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)), // Smooth transition
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);

            SetButtonStates("default", false, false, false);

            var worlds = DbChanger.SpecificDataFunc("SELECT worldNumber, Process_ID, startingStatus FROM worlds");
            var existingDirs = Directory.Exists(rootWorldsFolder)
                ? [.. Directory.GetDirectories(rootWorldsFolder).Select(Path.GetFileName)]
                : new HashSet<string>();

            ServerWorlds = [];

            foreach (var world in worlds)
            {
                string? worldNumber = world[0].ToString();

                if (string.IsNullOrEmpty(worldNumber) || !existingDirs.Contains(worldNumber))
                    continue; // Skip if world number is empty or directory doesn't exist

                string? processID = world[1]?.ToString();
                string? startingStatus = world[2]?.ToString();
                int IsRunning = 2;
                Color color = OfflineStatusColor;

                // Default button states
                bool ButtonState1 = true;
                bool ButtonState2 = false;
                bool ButtonState3 = false;

                if (!string.IsNullOrEmpty(startingStatus))
                {
                    IsRunning = 1;
                    color = LoadingStatusColor;
                    ButtonState1 = false;
                    ButtonState2 = false;
                    ButtonState3 = false;
                    serverRunning = true;
                }
                else if (!string.IsNullOrEmpty(processID))
                {
                    IsRunning = 0;
                    color = OnlineStatusColor;
                    ButtonState1 = false;
                    ButtonState2 = true;
                    ButtonState3 = true;
                    serverRunning = true;
                }

                if (!ServerWorlds.TryGetValue(worldNumber, out ServerWorld? worldValues))
                {
                    // Create a new ServerWorld if it doesn't exist
                    ServerWorlds[worldNumber] = new ServerWorld
                    {
                        IsActive = false,   // default to false when adding
                        IsRunning = IsRunning,
                        StatusColor = color,

                        ButtonState1 = ButtonState1,
                        ButtonState2 = ButtonState2,
                        ButtonState3 = ButtonState3
                    };
                }
                else
                {
                    worldValues.IsActive = false;   // default to false when adding
                    worldValues.IsRunning = IsRunning;
                    worldValues.StatusColor = color;

                    worldValues.ButtonState1 = ButtonState1;
                    worldValues.ButtonState2 = ButtonState2;
                    worldValues.ButtonState3 = ButtonState3;
                }

                //CodeLogger.ConsoleLog($"Name: {worldNumber}; Process_ID: '{processID}'; IsActive: false; IsRunning: {IsRunning};"); // For debugging
            }
        }

        // Open Control Panel Funcs
        private void OpenControlPanel(string serverName, string worldNumber)
        {
            if (ServerWorlds == null || !ServerWorlds.TryGetValue(worldNumber, out ServerWorld? world))
            {
                MessageBox.Show("Server not found or not loaded properly.");
                return;
            }

            selectedServer = serverName;
            openWorldNumber = worldNumber;
            serverNameBlock.Text = serverName;
            world.IsActive = true;

            if (world.IsRunning == 0)
            {
                SetConsoleOnline();
            }
            else
            {
                SetConsoleOffline();
            }

            viewModel = ViewModelManager.Get(worldNumber) ?? ViewModelManager.Create(worldNumber);
            this.DataContext = viewModel;

            AnimateStatusColor(world.StatusColor, worldNumber, 0);
            ShowButtonStates(worldNumber);

            if (string.IsNullOrEmpty(worldNumber))
            {
                MessageBox.Show("No world selected.");
                return;
            }
            if (rootWorldsFolder == null)
            {
                MessageBox.Show("Root worlds folder is not set.");
                return;
            }

            serverDirectoryPath = Path.Combine(rootWorldsFolder, worldNumber);
            CurrentPath = serverDirectoryPath;

            // Show control panel, hide server list
            MainContent.Visibility = Visibility.Collapsed;
            ServerDropBox.SelectedIndex = 0;
            ServerDropBox.Visibility = Visibility.Visible;

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
            if (string.IsNullOrEmpty(appDataPath))
            {
                throw new InvalidOperationException("The 'userData' path is not set.");
            }

            if (serverRunning)
            {
                PopupWindow.CreateStatusPopup("Info", $"A server is already running. Please stop it before trying to create a new one.", PopupHost);
                return;
            }

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

                // Server ports
                int Server_Port = 25565;
                int JMX_Port = 25562;
                int RMI_Port = 25563;
                int RCON_Port = 25575;
                // Server Memory Allocator
                int memoryAllocator = Convert.ToInt32(JsonHelper.GetOrSetValue(appDataPath, "runningServerMemory")?.ToString());

                // Utility for checkboxes
                static string GetCheckBoxValue(CheckBox cb, bool invert = false) =>
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

                string[] creationStatus = await Task.Run(() =>
                    ServerCreator.CreateServerFunc(
                    rootFolder, rootWorldsFolder, tempFolderPath, defaultServerPropertiesPath,
                    version, worldName, software, totalPlayers,
                    worldSettings, memoryAllocator,
                    localIP, Server_Port, JMX_Port, RCON_Port, RMI_Port
                    )
                );

                SetLoadingBarProgress(100);
                await Task.Delay(500);
                LoadingScreen.Visibility = Visibility.Collapsed;
                UnloadGIF();

                serverRunning = false;

                PopupWindow.CreateStatusPopup(creationStatus[0], creationStatus[1], PopupHost);

                if (creationStatus[0] == "Error" && creationStatus.Length == 3)
                {
                    ServerOperator.DeleteServer(creationStatus[2], Path.Combine(rootWorldsFolder, creationStatus[2])); // Auto-delete the server if creation failed
                    PopupWindow.CreateStatusPopup("Info", "Creation failed – Server auto-deleted.", PopupHost);
                }
            }
            catch (Exception ex)
            {
                if (rootFolder == null)
                {
                    MessageBox.Show("Root folder is not set.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CodeLogger.ConsoleLog($"Error creating the world. Error: {ex}");
                MessageBox.Show($"An unexpected error occurred. Check logs for details. Path: {Path.Combine(rootFolder, "logs\\latest.log")}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CreateServerBTN.IsEnabled = true;
                LoadServersPage();
            }
        }

        private async void StartServer(object sender, RoutedEventArgs e)
        {
            string worldNumber = "";
            if (openWorldNumber != null)
            {
                worldNumber = openWorldNumber;
            }

            if (serverRunning || processUnderWay)
            {
                PopupWindow.CreateStatusPopup("Info", $"A server is already in process. Please stop it before starting a new one.", PopupHost);
                return;
            }
            if (openWorldNumber == null)
            {
                MessageBox.Show("openWorldNumber is null.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            processUnderWay = true;

            DisableAllButtons(worldNumber);

            try
            {
                serverRunning = true;

                ServerWorld? wolrd = GetActiveWorld(worldNumber);

                AnimateStatusColor(LoadingStatusColor, worldNumber, 300);

                if (string.IsNullOrWhiteSpace(worldNumber) ||
                    string.IsNullOrWhiteSpace(rootWorldsFolder) ||
                    string.IsNullOrWhiteSpace(Server_PublicComputerIP) ||
                    string.IsNullOrWhiteSpace(serverDirectoryPath) ||
                    string.IsNullOrWhiteSpace(rootFolder) ||
                    viewModel == null ||
                    string.IsNullOrWhiteSpace(appDataPath))
                {
                    MessageBox.Show("Some required fields are not set. Cannot start the server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                object[] serverData = DbChanger.GetFunc(worldNumber)[0];

                string? serverName = serverData[2].ToString();
                int MainServerPort = Convert.ToInt32(serverData[6]);
                int JMX_Port = Convert.ToInt32(serverData[7]);
                int RCON_Port = Convert.ToInt32(serverData[8]);
                int RMI_Port = Convert.ToInt32(serverData[9]);

                string status = "Error";

                object[] outputID = [0, "Unknown error"];

                if (_activeMonitors.TryGetValue(worldNumber, out var oldMonitor))
                {
                    await oldMonitor.StopMonitoringAsync();
                    _activeMonitors.Remove(worldNumber);
                }

                ServerMonitor monitor = new(viewModel, worldNumber);
                _activeMonitors[worldNumber] = monitor;

                await Task.Run(async () =>
                {
                    // Start the server
                    outputID = await ServerOperator.Start(
                    worldNumber,
                    serverDirectoryPath,
                    processMemoryAlocation: Convert.ToInt32(JsonHelper.GetOrSetValue(appDataPath, "runningServerMemory")?.ToString() ?? "5000"),
                    Server_PublicComputerIP,
                    MainServerPort,
                    JMX_Port,
                    RCON_Port,
                    RMI_Port,
                    noGUI: noGUIonServerStartup,
                    onServerRunning: (_) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            string serverWorldPath = Path.Combine(rootWorldsFolder, worldNumber);

                            object[] userDataTemp = DbChanger.SpecificDataFunc($"SELECT serverUser, serverTempPsw FROM worlds WHERE worldNumber == \"{worldNumber}\";")[0];
                            string[] userData = [$"{userDataTemp[0]}", $"{userDataTemp[1]}"];

                            monitor.StartMonitoring(worldNumber, serverData, userData, serverWorldPath, Server_PublicComputerIP, JMX_Port, MainServerPort);

                            DbChanger.SpecificDataFunc($"UPDATE worlds SET startingStatus = NULL WHERE worldNumber = '{worldNumber}';");

                            status = "Success";

                            ChangeButtonStates(worldNumber);
                            AnimateStatusColor(OnlineStatusColor, worldNumber, 300);

                            PopupWindow.CreateStatusPopup(status, $"Server \"{serverName}\" has started successfully.", PopupHost);
                        });
                    }
                );
                });

                if (Convert.ToInt32(outputID[0]) != 0)
                {
                    PopupWindow.CreateStatusPopup("Error", $"{outputID[1]}", PopupHost);
                    serverRunning = false;
                    SetDefaultButtonStates(worldNumber);
                    AnimateStatusColor(OfflineStatusColor, worldNumber, 300);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while starting the server:\n{ex.Message}", "Start Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetDefaultButtonStates(worldNumber);
                serverRunning = false;
                if (_activeMonitors.TryGetValue(worldNumber, out var oldMonitor))
                {
                    await oldMonitor.StopMonitoringAsync();
                    _activeMonitors.Remove(worldNumber);
                }
            }
            finally
            {
                processUnderWay = false;
            }
        }

        private async void StopServer(object sender, RoutedEventArgs e)
        {
            string worldNumber = "";
            if (openWorldNumber != null)
            {
                worldNumber = openWorldNumber;
            }

            processUnderWay = true;

            DisableAllButtons(worldNumber);

            try
            {
                AnimateStatusColor(LoadingStatusColor, worldNumber, 300);

                if (string.IsNullOrWhiteSpace(worldNumber) ||
                    viewModel == null ||
                    string.IsNullOrWhiteSpace(Server_LocalComputerIP))
                {
                    MessageBox.Show("Some required fields are not set. Cannot stop the server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                object[] serverData = DbChanger.GetFunc(worldNumber)[0];

                string? serverName = serverData[2].ToString();
                int RCON_Port = Convert.ToInt32(serverData[8]);

                string status = "Success";

                //if (_activeMonitors.TryGetValue(worldNumber, out var oldMonitor))
                //{
                //    await oldMonitor.StopMonitoringAsync();
                //    _activeMonitors.Remove(worldNumber);
                //    ServerMonitor.VMDefaultValues(worldNumber, viewModel);
                //}

                if (_activeMonitors.TryGetValue(worldNumber, out var oldMonitor))
                {
                    StopAllMonitors();
                    ServerMonitor.VMDefaultValues(worldNumber, viewModel);
                }

                await Task.Run(async () =>
                {
                    // Stop the server
                    await ServerOperator.Stop(
                    "Stop",
                    worldNumber,
                    Server_LocalComputerIP,
                    RCON_Port
                );
                });

                serverRunning = false; // Assuring the server stops successfully
                ChangeButtonStates(worldNumber);
                AnimateStatusColor(OfflineStatusColor, worldNumber, 300);

                PopupWindow.CreateStatusPopup(status, $"Server \"{serverName}\" has been stopped.", PopupHost);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while stopping the server:\n{ex.Message}", "Stop Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetDefaultButtonStates(worldNumber);
            }
            finally
            {
                processUnderWay = false;
            }
        }

        private async void RestartServer(object sender, RoutedEventArgs e)
        {
            string worldNumber = "";
            if (openWorldNumber != null)
            {
                worldNumber = openWorldNumber;
            }

            processUnderWay = true;

            DisableAllButtons(worldNumber);

            try
            {
                AnimateStatusColor(LoadingStatusColor, worldNumber, 300);

                if (string.IsNullOrWhiteSpace(worldNumber) ||
                    string.IsNullOrWhiteSpace(rootWorldsFolder) ||
                    string.IsNullOrWhiteSpace(Server_PublicComputerIP) ||
                    string.IsNullOrWhiteSpace(Server_LocalComputerIP) ||
                    string.IsNullOrWhiteSpace(serverDirectoryPath) ||
                    string.IsNullOrWhiteSpace(rootFolder) ||
                    string.IsNullOrWhiteSpace(appDataPath) ||
                    viewModel == null)
                {
                    MessageBox.Show("Some required fields are not set. Cannot restart the server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                object[] serverData = DbChanger.GetFunc(worldNumber)[0];
                string? serverName = serverData[2].ToString();
                int MainServerPort = Convert.ToInt32(serverData[6]);
                int JMX_Port = Convert.ToInt32(serverData[7]);
                int RCON_Port = Convert.ToInt32(serverData[8]);
                int RMI_Port = Convert.ToInt32(serverData[9]);

                if (_activeMonitors.TryGetValue(worldNumber, out var oldMonitor))
                {
                    await oldMonitor.StopMonitoringAsync();
                    _activeMonitors.Remove(worldNumber);
                }

                await Task.Run(async () =>
                {
                    await ServerOperator.Stop("Stop", worldNumber, Server_LocalComputerIP, RCON_Port);
                });


                ServerMonitor.VMDefaultValues(worldNumber, viewModel);

                serverRunning = true;
                string status = "Error";
                object[] outputID = [0, "Unknown error"];

                ServerMonitor monitor = new(viewModel, worldNumber);
                _activeMonitors[worldNumber] = monitor;

                await Task.Run(async () =>
                {
                    outputID = await ServerOperator.Start(
                        worldNumber,
                        serverDirectoryPath,
                        processMemoryAlocation: Convert.ToInt32(JsonHelper.GetOrSetValue(appDataPath, "runningServerMemory")?.ToString() ?? "5000"),
                        Server_PublicComputerIP,
                        MainServerPort,
                        JMX_Port,
                        RCON_Port,
                        RMI_Port,
                        noGUI: noGUIonServerStartup,
                        onServerRunning: (_) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                string serverWorldPath = Path.Combine(rootWorldsFolder, worldNumber);

                                object[] userDataTemp = DbChanger.SpecificDataFunc($"SELECT serverUser, serverTempPsw FROM worlds WHERE worldNumber == \"{worldNumber}\";")[0];
                                string[] userData = [$"{userDataTemp[0]}", $"{userDataTemp[1]}"];

                                monitor.StartMonitoring(worldNumber, serverData, userData, serverWorldPath, Server_PublicComputerIP, JMX_Port, MainServerPort);

                                DbChanger.SpecificDataFunc($"UPDATE worlds SET startingStatus = NULL WHERE worldNumber = '{worldNumber}';");

                                status = "Success";

                                ChangeButtonStates(worldNumber);
                                AnimateStatusColor(OnlineStatusColor, worldNumber, 300);

                                PopupWindow.CreateStatusPopup(status, $"Server \"{serverName}\" has restarted successfully.", PopupHost);
                            });
                        }
                    );
                });

                if (Convert.ToInt32(outputID[0]) != 0)
                {
                    PopupWindow.CreateStatusPopup("Error", $"{outputID[1]}", PopupHost);
                    serverRunning = false;
                    SetDefaultButtonStates(worldNumber);
                    AnimateStatusColor(OfflineStatusColor, worldNumber, 300);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while restarting the server:\n{ex.Message}", "Restart Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetDefaultButtonStates(worldNumber);
                serverRunning = false;

                if (_activeMonitors.TryGetValue(worldNumber, out var oldMonitor))
                {
                    await oldMonitor.StopMonitoringAsync();
                    _activeMonitors.Remove(worldNumber);
                }
            }
            finally
            {
                processUnderWay = false;
            }
        }

        // Helper methods for button states and world handling
        public async void StopAllMonitors()
        {
            foreach (var monitor in _activeMonitors.Values)
            {
                await monitor.StopMonitoringAsync();
            }

            _activeMonitors.Clear();
            ViewModelManager.ClearAll();
        }

        private ServerWorld? GetActiveWorld(string worldNumber)
        {
            if (worldNumber == null)
            {
                MessageBox.Show("openWorldNumber not found or not loaded properly.");
                return null;
            }

            if (ServerWorlds == null || !ServerWorlds.TryGetValue(worldNumber, out ServerWorld? world))
            {
                MessageBox.Show("Server not found or not loaded properly.");
                return null;
            }

            return world;
        }

        private void ShowButtonStates(string worldNumber)
        {
            var world = GetActiveWorld(worldNumber);
            if (world == null) return;

            SetButtonStates(worldNumber, world.ButtonState1, world.ButtonState2, world.ButtonState3);
        }

        private void DisableAllButtons(string worldNumber)
        {
            var world = GetActiveWorld(worldNumber);
            if (world == null) return;

            world.ButtonState1 = false;
            world.ButtonState2 = false;
            world.ButtonState3 = false;

            SetButtonStates(worldNumber, false, false, false);
        }

        private void ChangeButtonStates(string worldNumber)
        {
            var world = GetActiveWorld(worldNumber);
            if (world == null) return;

            if (serverRunning)
            {
                world.ButtonState1 = false;
                world.ButtonState2 = true;
                world.ButtonState3 = true;
                world.IsRunning = 0;
            }
            else
            {
                world.ButtonState1 = true;
                world.ButtonState2 = false;
                world.ButtonState3 = false;
                world.IsRunning = 2;
            }

            SetButtonStates(worldNumber, world.ButtonState1, world.ButtonState2, world.ButtonState3);
        }

        private void SetDefaultButtonStates(string worldNumber)
        {
            var world = GetActiveWorld(worldNumber);
            if (world == null) return;

            world.ButtonState1 = true;
            world.ButtonState2 = false;
            world.ButtonState3 = false;

            SetButtonStates(worldNumber, true, false, false);
        }

        private void SetButtonStates(string worldNumber, bool ButtonState1, bool ButtonState2, bool ButtonState3)
        {
            if (worldNumber == openWorldNumber || worldNumber == "default")
            {
                StartBtn.IsEnabled = ButtonState1;
                StopBtn.IsEnabled = ButtonState2;
                RestartBtn.IsEnabled = ButtonState3;
            }
        }

        public void SetConsoleOnline()
        {
            ConsoleTextBox.Visibility = Visibility.Visible;
            ConsoleTextBoxOfflineStatus.Visibility = Visibility.Hidden;
        }

        public void SetConsoleOffline()
        {
            ConsoleTextBox.Visibility = Visibility.Hidden;
            ConsoleTextBoxOfflineStatus.Visibility = Visibility.Visible;
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

                    if (selectedName == "Files")
                    {
                        List<List<string>> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(CurrentPath);
                        FileExplorerCards.CreateExplorerItems(ExplorerParent, Files_Folders, pathContainer);
                    }
                    else if (selectedName == "Console")
                    {
                        ConsoleInput.Text = string.Empty;
                        ConsoleTextBoxScrollBar.ScrollToEnd();

                        if (openWorldNumber != null && viewModel != null)
                        {
                            if (ServerWorlds == null || !ServerWorlds.TryGetValue(openWorldNumber, out ServerWorld? world))
                            {
                                MessageBox.Show("Server not found or not loaded properly.");
                                return;
                            }

                            // Check if the server is running (1 = starting/loading, 2 = offline)
                            if (world.IsRunning == 1 || world.IsRunning == 2)
                            {
                                // Reset the ViewModel to default values
                                ServerMonitor.VMDefaultValues(openWorldNumber, viewModel, false);
                            }
                        }
                    }
                }
            }
        }

        // Send Command to Server
        private async void SendConsoleCommand(object sender, RoutedEventArgs e)
        {
            string? command = ConsoleInput.Text?.Trim();

            if (string.IsNullOrEmpty(openWorldNumber))
            {
                MessageBox.Show("No world is currently open (openWorldNumber is null or empty).", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!serverRunning)
            {
                PopupWindow.CreateStatusPopup("Info", "Server is not running.", PopupHost);
                return;
            }
            if (string.IsNullOrEmpty(command))
            {
                PopupWindow.CreateStatusPopup("Info", "Please enter a command.", PopupHost);
                return;
            }
            if (command.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                PopupWindow.CreateStatusPopup("Info", "Stop the server from the button in the manager tab.", PopupHost);
                return;
            }

            try
            {
                string query = $"SELECT RCON_Port FROM worlds WHERE worldNumber = {openWorldNumber};";
                var rconPortData = DbChanger.SpecificDataFunc(query);

                if (rconPortData == null || rconPortData.Count == 0 || rconPortData[0].Length == 0)
                {
                    PopupWindow.CreateStatusPopup("Error", "Failed to retrieve the RCON port from the database.", PopupHost);
                    return;
                }

                string? RCON_Port = rconPortData[0][0]?.ToString();
                if (!int.TryParse(RCON_Port, out int rconPort))
                {
                    PopupWindow.CreateStatusPopup("Error", "Invalid RCON port retrieved from database.", PopupHost);
                    return;
                }

                if (string.IsNullOrEmpty(Server_LocalComputerIP))
                {
                    PopupWindow.CreateStatusPopup("Error", "Local IP address is not set.", PopupHost);
                    return;
                }

                // Send command and capture any error
                string? error = await ServerOperator.InputForServer(command, openWorldNumber, rconPort, Server_LocalComputerIP);

                if (error != null)
                {
                    PopupWindow.CreateStatusPopup("Error", error, PopupHost);
                    return;
                }

                ConsoleInput.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred:\n\n{ex.Message}", "Command Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                loadingScreenProcentage = percentage;
            });
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

        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                    yield return t;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }

        // To do: Implement the following methods for file operations
        private async void ExplorerArhiveBtn(object sender, RoutedEventArgs e)
        {
            try
            {
                ArhiveBtn.IsEnabled = false;
                DeleteBtn.IsEnabled = false;

                if (string.IsNullOrEmpty(appDataPath))
                {
                    MessageBox.Show("The 'userData' path is not set.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                object[] selectedItems = FileExplorerCards.ShowSelectedItems(ExplorerParent);
                List<string> itemPaths = selectedItems[2] as List<string> ?? [];
                string downloadsPath = JsonHelper.GetOrSetValue(appDataPath, "archivePath")?.ToString() ?? string.Empty;

                string[] archivingStatus = await Task.Run(() => AchiveExplorerItems(itemPaths, downloadsPath));

                PopupWindow.CreateStatusPopup(archivingStatus[0], archivingStatus[1], PopupHost);
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error archiving items: {ex.Message}");
                MessageBox.Show($"An error occurred while archiving items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                List<List<string>> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(CurrentPath);
                FileExplorerCards.CreateExplorerItems(ExplorerParent, Files_Folders, pathContainer);

                await Task.Delay(300);

                ArhiveBtn.IsEnabled = true;
                DeleteBtn.IsEnabled = true;
            }
        }

        private async void ExplorerDeleteBtn(object sender, RoutedEventArgs e)
        {
            try
            {
                ArhiveBtn.IsEnabled = false;
                DeleteBtn.IsEnabled = false;

                object[] selectedItems = FileExplorerCards.ShowSelectedItems(ExplorerParent);
                List<string> itemPaths = selectedItems[2] as List<string> ?? [];

                string[] deletingStatus = await Task.Run(() => DeleteExplorerItems(itemPaths));

                PopupWindow.CreateStatusPopup(deletingStatus[0], deletingStatus[1], PopupHost);
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error deleting items: {ex.Message}");
                MessageBox.Show($"An error occurred while deleting items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                List<List<string>> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(CurrentPath);
                FileExplorerCards.CreateExplorerItems(ExplorerParent, Files_Folders, pathContainer);

                await Task.Delay(300);

                ArhiveBtn.IsEnabled = true;
                DeleteBtn.IsEnabled = true;
            }
        }

        private static string[] DeleteExplorerItems(List<string> items)
        {
            if (items == null || items.Count == 0)
                return ["Error", "No items provided for deletion."];

            List<string> errors = [];
            int deletedCount = 0;

            foreach (string item in items)
            {
                try
                {
                    if (Directory.Exists(item))
                    {
                        Directory.Delete(item, true);
                        deletedCount++;
                    }
                    else if (File.Exists(item))
                    {
                        File.Delete(item);
                        deletedCount++;
                    }
                    else
                    {
                        errors.Add($"Item not found: {item}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to delete '{item}': {ex.Message}");
                }
            }

            if (errors.Count == 0)
            {
                return ["Success", $"All {deletedCount} items deleted successfully."];
            }
            else if (deletedCount > 0)
            {
                return ["Info", $"{deletedCount} items deleted. {errors.Count} failed. Details: {string.Join(" | ", errors)}"];
            }
            else
            {
                return ["Error", $"No items deleted. Errors: {string.Join(" | ", errors)}"];
            }
        }

        public static string[] AchiveExplorerItems(List<string> paths, string destinationFolder)
        {
            try
            {
                if (paths == null || paths.Count == 0)
                    return ["Error", "No items provided to archive."];

                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                // Generate unique zip file name
                string baseName = "archive";
                string zipPath = Path.Combine(destinationFolder, baseName + ".zip");
                int counter = 1;
                while (File.Exists(zipPath))
                {
                    zipPath = Path.Combine(destinationFolder, $"{baseName} ({counter}).zip");
                    counter++;
                }

                // Temp folder to stage files/folders
                string tempRoot = Path.Combine(Path.GetTempPath(), "ArchiveTemp_" + Guid.NewGuid());
                Directory.CreateDirectory(tempRoot);

                foreach (string path in paths)
                {
                    if (File.Exists(path))
                    {
                        string fileName = Path.GetFileName(path);
                        File.Copy(path, Path.Combine(tempRoot, fileName), true);
                    }
                    else if (Directory.Exists(path))
                    {
                        string folderName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
                        string destFolder = Path.Combine(tempRoot, folderName);
                        DirectoryCopy(path, destFolder, true);
                    }
                    else
                    {
                        return ["Error", $"Path does not exist: {path}"];
                    }
                }

                // Create zip
                ZipFile.CreateFromDirectory(tempRoot, zipPath);

                // Clean up temp
                Directory.Delete(tempRoot, true);

                return ["Success", $"Archive created successfully at: {zipPath}"];
            }
            catch (Exception ex)
            {
                return ["Error", ex.Message];
            }

        }

        private static void DirectoryCopy(string sourceDir, string destDir, bool copySubDirs)
        {
            DirectoryInfo dir = new(sourceDir);
            if (!dir.Exists)
                throw new DirectoryNotFoundException("Source directory not found: " + sourceDir);

            Directory.CreateDirectory(destDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dir.GetDirectories())
                {
                    string newDestDir = Path.Combine(destDir, subdir.Name);
                    DirectoryCopy(subdir.FullName, newDestDir, copySubDirs);
                }
            }
        }

        // TextBox Mouse Wheel Scrolling Handler
        private void TextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Check if Shift is pressed (standard for horizontal scrolling)
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    // Scroll horizontally using offset
                    var scrollViewer = GetScrollViewer(textBox);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
                        e.Handled = true;
                    }
                }
            }
        }

        private static ScrollViewer? GetScrollViewer(DependencyObject o)
        {
            if (o is ScrollViewer viewer) return viewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private void ConsoleScrollUI(object sender, TextChangedEventArgs e)
        {
            ConsoleTextBox.ScrollToEnd();
        }
    }
}