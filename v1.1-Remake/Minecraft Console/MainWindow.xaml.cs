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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
                Source = new BitmapImage(new Uri(System.IO.Path.Combine(rootFolder, $"assets\\icons\\folder_file\\{Type}.png")))
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

        public static void AddTextEditorItem(ListView listView, string filePath, string fileContent)
        {
            // Clear ListView
            listView.Items.Clear();

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
            string fileName = System.IO.Path.GetFileName(filePath);

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

            string combinedPath = System.IO.Path.Combine(CurrentPath, fileName);
            LoadFiles(combinedPath);
        }

        private void OpenFile(string fileName)
        {
            if (CurrentPath == null)
            {
                MessageBox.Show("Current path is null.");
                return;
            }

            string combinedPath = System.IO.Path.Combine(CurrentPath, fileName);

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
            string fileExtension = System.IO.Path.GetExtension(combinedPath).ToLower();

            CodeLogger.ConsoleLog($"fileExtension: {fileExtension}");
            string? fileContent = ServerFileExplorer.ReadFromFile(combinedPath);
            if (fileContent == null)
            {
                return;
            }

            // Check if the extension is supported
            if (supportedExtensions.Contains(fileExtension))
            {
                CurrentPath = combinedPath;
                AddTextEditorItem(Files_Folders_ListView, combinedPath, fileContent);
                DisplayPathComponents(pathContainer, CurrentPath);
            }
            else
            {
                MessageBox.Show($"File type '{fileExtension}' is not supported.", "Unsupported File", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
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

            double buttonWidth, buttonHeight;
            (buttonWidth, buttonHeight) = GetButtonDimensions(MainContent.ActualWidth, serverPanel.Children.Count + 1); // +1 for CreateServer

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

                var button = CreateStyledButton(cornerRadius, worldName, worldVersion, worldTotalPlayers, processID, buttonMargin, () => OpenControlPanel(worldName, worldNumber), buttonWidth, buttonHeight);
                serverPanel.Children.Add(button);
            }

            var createButton = CreateStyledCreateButton(buttonMargin, cornerRadius, CreateServerButton_Click, buttonWidth, buttonHeight);
            serverPanel.Children.Add(createButton);

            scrollViewer.Content = serverPanel;
            MainContent.Children.Add(scrollViewer);

            HideAllServerInfoGrids(true);
            MainContent.Visibility = Visibility.Visible;
            CreateServerPage.Visibility = Visibility.Collapsed;

            SizeChanged += (s, e) => UpdateButtonSizes(serverPanel); // handle resize
            UpdateButtonSizes(serverPanel); // initial sizing
        }

        // Funcs for showing the created servers
        private void UpdateButtonSizes(WrapPanel panel, double shrinkIntensity = 2.0)
        {
            double containerWidth = MainContent.ActualWidth;
            double buttonMarginThickness = 50;
            double minButtonWidth = 250;
            double aspectRatio = 3.0;

            int numberOfButtons = panel.Children.Count;
            if (numberOfButtons == 0) return;

            double availableWidth = containerWidth - (panel.Margin.Left + panel.Margin.Right);
            double totalMargin = numberOfButtons * buttonMarginThickness;
            double baseButtonWidth = Math.Max(minButtonWidth, (availableWidth - totalMargin) / numberOfButtons);
            double baseButtonHeight = baseButtonWidth / aspectRatio + 50;

            // Scaling factors
            double titleFontScale = 0.075;
            double infoFontScale = 0.06;
            double statusFontScale = 0.11;
            double statusGridWidthScale = 0.25;
            double statusGridHeightScale = 0.15;
            double statusMarginScale = 0.05;
            double statusInnerMarginScale = 0.1;
            double plusSignWidthScale = 0.05;
            double plusSignHeightScale = 0.05;
            double circleSizeScale = 0.16;
            double plusThicknessScale = 0.001;

            // Max factors
            double maxTitleFontSize = 40.0;
            double maxFontSize = 25.0;
            double maxSatusFontSize = 60.0;
            double maxGridWidth = 130.0;
            double maxPlusSize = 25.0;
            double maxCircleSize = 70.0;

            foreach (Button btn in panel.Children)
            {
                btn.MinWidth = minButtonWidth;
                btn.Width = baseButtonWidth;
                btn.Height = baseButtonHeight;
                btn.HorizontalAlignment = HorizontalAlignment.Stretch;

                double scaledHeight = btn.Height * shrinkIntensity;

                if (btn.Content is not Grid grid) continue;

                // Get references to the TextBlocks once per button
                TextBlock? nameText = FindChildByName<TextBlock>(grid, "ServerNameVisualLabel", true);
                TextBlock? versionText = FindChildByName<TextBlock>(grid, "ServerVersionVisualisationLabel", true);
                TextBlock? playersText = FindChildByName<TextBlock>(grid, "ServerTotalPlayersVisualisationLabel", true);
                Grid? statusGrid = FindChildByName<Grid>(grid, "GridForON_OFF_Status", true);
                TextBlock? createServerTextBlock = FindChildByName<TextBlock>(grid, "CreateServerTextBlock", true);
                Ellipse? createServerCircle = FindChildByName<Ellipse>(grid, "CreateServerCircle", true);
                Rectangle? plusHorizontal = FindChildByName<Rectangle>(grid, "CreateServerPlusHorizontal", true);
                Rectangle? plusVertical = FindChildByName<Rectangle>(grid, "CreateServerPlusVertical", true);

                // Resize text elements
                if (nameText != null)
                {
                    nameText.FontSize = Math.Min(maxTitleFontSize, scaledHeight * titleFontScale);
                }

                if (versionText != null)
                {
                    versionText.FontSize = Math.Min(maxFontSize, scaledHeight * infoFontScale);
                }

                if (playersText != null)
                {
                    playersText.FontSize = Math.Min(maxFontSize, scaledHeight * infoFontScale);
                }

                if (createServerTextBlock != null)
                {
                    createServerTextBlock.FontSize = Math.Min(maxTitleFontSize, scaledHeight * titleFontScale);
                }

                // Resize status grid and its content
                if (statusGrid != null)
                {
                    double gridWidthCalculated = scaledHeight * statusGridWidthScale;
                    double gridHeightCalculated = scaledHeight * statusGridHeightScale;

                    double gridWidth = Math.Min(maxGridWidth, gridWidthCalculated);
                    double gridHeight = gridWidth / (statusGridWidthScale / statusGridHeightScale); // Maintain aspect ratio

                    double margin = scaledHeight * statusMarginScale;
                    statusGrid.Width = gridWidth;
                    statusGrid.Height = gridHeight;
                    statusGrid.Margin = new Thickness(0, 0, margin, 0);

                    if (statusGrid.Children.OfType<Border>().FirstOrDefault() is Border border)
                    {
                        border.Width = gridWidth;
                        border.Height = gridHeight;

                        if (border.Child is TextBlock statusText)
                        {
                            statusText.FontSize = Math.Min(maxSatusFontSize, scaledHeight * statusFontScale);
                            double innerMarginValue = gridHeight * statusInnerMarginScale;
                            statusText.Margin = new Thickness(0, innerMarginValue, 0, 0);
                        }
                    }
                }

                // Resize the Plus
                if (createServerCircle != null)
                {
                    double circleSize = Math.Min(maxCircleSize, scaledHeight * circleSizeScale);
                    createServerCircle.Width = circleSize;
                    createServerCircle.Height = circleSize;
                }

                if (plusHorizontal != null)
                {
                    double plusWidth = Math.Min(maxPlusSize, scaledHeight * plusSignWidthScale);
                    double plusHeight = Math.Max(2, scaledHeight * plusThicknessScale);
                    plusHorizontal.Width = plusWidth;
                    plusHorizontal.Height = plusHeight;
                }

                if (plusVertical != null)
                {
                    double plusWidth = Math.Max(2, scaledHeight * plusThicknessScale);
                    double plusHeight = Math.Min(maxPlusSize, scaledHeight * plusSignHeightScale);
                    plusVertical.Width = plusWidth;
                    plusVertical.Height = plusHeight;
                }
            }
        }

        public static void ApplyButtonStyle(Button button, int cornerRadius)
        {
            // Create the Border element that will be used in the ControlTemplate
            var borderFactory = new FrameworkElementFactory(typeof(Border))
            {
                Name = "border"
            };
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(cornerRadius));
            borderFactory.SetValue(Border.ClipToBoundsProperty, true);
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));

            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(MarginProperty, new TemplateBindingExtension(PaddingProperty));
            contentPresenterFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            contentPresenterFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Stretch);

            borderFactory.AppendChild(contentPresenterFactory);

            // Create ControlTemplate with trigger for IsMouseOver
            var template = new ControlTemplate(typeof(Button))
            {
                VisualTree = borderFactory
            };

            // Add trigger for mouse over
            var trigger = new Trigger
            {
                Property = IsMouseOverProperty,
                Value = true
            };
            trigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(47, 51, 61)), "border"));

            template.Triggers.Add(trigger);

            // Create the Style
            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(38, 42, 50))));
            style.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0)));
            style.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            style.Setters.Add(new Setter(FontSizeProperty, 25.0));
            style.Setters.Add(new Setter(TemplateProperty, template));

            // Apply style to the button
            button.Style = style;
        }

        private static Button CreateStyledCreateButton(Thickness margin, int cornerRadius, Action onClick, double width, double height)
        {
            var button = new Button
            {
                Background = (Brush?)new BrushConverter().ConvertFrom("#262A32") ?? Brushes.White,
                Foreground = Brushes.White,
                FontSize = 25,
                Cursor = Cursors.Hand,
                Margin = margin,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                BorderThickness = new Thickness(0),
                ClipToBounds = true,
                Width = width,
                Height = height,
                MaxWidth = 500,   // Optional - safety limit
                MaxHeight = 250,  // Optional - safety limit
            };

            ApplyButtonStyle(button, cornerRadius);
            button.MouseEnter += (s, e) => ApplyHoverEffect(button, true);
            button.MouseLeave += (s, e) => ApplyHoverEffect(button, false);

            button.Content = CreateButtonInnerGrid();

            button.Click += (s, e) => onClick();
            return button;
        }

        private static Button CreateStyledButton(int cornerRadius, string name, string version, string totalPlayers, string processID, Thickness margin, Action onClick, double width, double height)
        {
            var button = new Button
            {
                Background = (Brush?)new BrushConverter().ConvertFrom("#262A32") ?? Brushes.White,
                Foreground = Brushes.White,
                FontSize = 25,
                Cursor = Cursors.Hand,
                Margin = margin,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                BorderThickness = new Thickness(0),
                ClipToBounds = true,
                Width = width,
                Height = height,
                MaxWidth = 500,   // Optional - safety limit
                MaxHeight = 250,  // Optional - safety limit
            };

            ApplyButtonStyle(button, cornerRadius);
            button.MouseEnter += (s, e) => ApplyHoverEffect(button, true);
            button.MouseLeave += (s, e) => ApplyHoverEffect(button, false);

            button.Content = ButtonInnerGrid(name, version, totalPlayers, processID);

            button.Click += (s, e) => onClick();
            return button;
        }

        public static Grid CreateButtonInnerGrid()
        {
            // Create the main Grid
            Grid mainGrid = new();
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            // Create the TextBlock for "Create Server"
            TextBlock textBlock = new()
            {
                Name = "CreateServerTextBlock",
                Text = "Create Server",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(textBlock, 0);
            mainGrid.Children.Add(textBlock);

            // Create the Grid for the circle and plus
            Grid bottomGrid = new()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetRow(bottomGrid, 1);
            mainGrid.Children.Add(bottomGrid);

            // Create the circle
            Ellipse circle = new()
            {
                Name = "CreateServerCircle",
                Width = 45,
                Height = 45,
                Fill = new SolidColorBrush(Color.FromRgb(217, 217, 217)) // #D9D9D9
            };
            bottomGrid.Children.Add(circle);

            // Create the plus sign (as two rectangles)
            Rectangle plusHorizontal = new()
            {
                Name = "CreateServerPlusHorizontal",
                Width = 14,
                Height = 2,
                Fill = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            bottomGrid.Children.Add(plusHorizontal);

            Rectangle plusVertical = new()
            {
                Name = "CreateServerPlusVertical",
                Width = 2,
                Height = 14,
                Fill = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            bottomGrid.Children.Add(plusVertical);

            return mainGrid;
        }

        private static Grid ButtonInnerGrid(string name, string version, string totalPlayers, string processID)
        {
            var mainGrid = new Grid();

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.8, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Top grid with image and label
            var topGrid = new Grid();
            Grid.SetRow(topGrid, 0);

            var image = new Image
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Source = rootFolder != null
                    ? new BitmapImage(new Uri(System.IO.Path.Combine(rootFolder, "assets\\Images\\serverBtnImage.png")))
                    : throw new InvalidOperationException("Root folder is not set."),
                Name = "IndividualServerImage"
            };

            void UpdateClip()
            {
                image.Clip = new RectangleGeometry
                {
                    RadiusX = 20,
                    RadiusY = 20,
                    Rect = new Rect(0, 0, image.ActualWidth, image.ActualHeight)
                };
            }

            image.Loaded += (s, e) => UpdateClip();
            image.SizeChanged += (s, e) => UpdateClip();

            topGrid.Children.Add(image);

            var topBorder = new Border
            {
                Margin = new Thickness(0, 0, 0, -2),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = new BrushConverter().ConvertFrom("#262A32") as Brush,
                CornerRadius = new CornerRadius(0, 25, 0, 0)
            };

            // Create hover style
            var borderStyle = new Style(typeof(Border));
            borderStyle.Setters.Add(new Setter(Border.BackgroundProperty, new BrushConverter().ConvertFrom("#262A32")));
            borderStyle.Triggers.Add(new Trigger
            {
                Property = Border.IsMouseOverProperty,
                Value = true,
                Setters = {
                    new Setter(Border.BackgroundProperty, new BrushConverter().ConvertFrom("#2F333D"))
                }
            });
            topBorder.Style = borderStyle;

            var serverNameText = new TextBlock
            {
                Name = "ServerNameVisualLabel",
                Padding = new Thickness(10, 10, 15, 6),
                FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./assets/Fonts/Itim/#Itim"),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Text = name
            };

            topBorder.Child = serverNameText;
            topGrid.Children.Add(topBorder);

            mainGrid.Children.Add(topGrid);

            // Bottom grid with version, players and ON/OFF status
            var bottomGrid = new Grid();
            Grid.SetRow(bottomGrid, 1);

            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var leftInfoGrid = new Grid();
            leftInfoGrid.RowDefinitions.Add(new RowDefinition());
            leftInfoGrid.RowDefinitions.Add(new RowDefinition());

            var versionText = new TextBlock
            {
                FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./assets/Fonts/Itim/#Itim"),
                Name = "ServerVersionVisualisationLabel",
                Margin = new Thickness(10, 0, 0, 15),
                VerticalAlignment = VerticalAlignment.Bottom,
                FontSize = 18,
                Foreground = Brushes.White,
                Text = $"Version: {version}"
            };
            Grid.SetRow(versionText, 0);

            var playersText = new TextBlock
            {
                FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./assets/Fonts/Itim/#Itim"),
                Name = "ServerTotalPlayersVisualisationLabel",
                Margin = new Thickness(10, 0, 0, 15),
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = 18,
                Foreground = Brushes.White,
                Text = $"Total Players: {totalPlayers}"
            };
            Grid.SetRow(playersText, 1);

            leftInfoGrid.Children.Add(versionText);
            leftInfoGrid.Children.Add(playersText);

            Grid.SetColumn(leftInfoGrid, 0);
            bottomGrid.Children.Add(leftInfoGrid);

            var statusGrid = new Grid
            {
                Name = "GridForON_OFF_Status",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0, 0, 30, 0) // Add a right margin of 30
            };
            Grid.SetColumn(statusGrid, 1);

            string[] serverStatus = GetServerStatus(processID);

            var statusBorder = new Border
            {
                Width = 100,
                Height = 55,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new BrushConverter().ConvertFrom("#3B414D") as Brush,
                CornerRadius = new CornerRadius(5)
            };

            var statusText = new TextBlock
            {
                FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./assets/Fonts/Jomhuria/#Jomhuria"),
                Margin = new Thickness(0, 7, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 50,
                Foreground = new BrushConverter().ConvertFrom(serverStatus[1]) as Brush,
                Text = serverStatus[0]
            };

            statusBorder.Child = statusText;
            statusGrid.Children.Add(statusBorder);
            bottomGrid.Children.Add(statusGrid);

            mainGrid.Children.Add(bottomGrid);

            return mainGrid;
        }

        private static void ApplyHoverEffect(Button button, bool isHovered)
        {
            if (button.Content is Grid mainGrid)
            {
                // Imaginea
                var image = FindChild<Image>(mainGrid, "IndividualServerImage");
                if (image != null)
                {
                    image.Opacity = isHovered ? 0.85 : 1.0;
                }

                // Borderul cu text
                var nameBorder = FindChild<Border>(mainGrid, null, child =>
                {
                    return child is Border b && b.Child is TextBlock tb && tb.Name == "ServerNameVisualLabel";
                });
                if (nameBorder != null)
                {
                    nameBorder.Background = isHovered
                        ? new SolidColorBrush(Color.FromRgb(47, 51, 61))  // #2F333D
                        : new SolidColorBrush(Color.FromRgb(38, 42, 50)); // #262A32
                }
            }
        }

        private static T? FindChildByName<T>(DependencyObject parent, string name, bool searchInChildren = false) where T : FrameworkElement
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && typedChild.Name == name)
                    return typedChild;

                var result = FindChildByName<T>(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static T? FindChild<T>(DependencyObject parent, string? name = null, Func<T, bool>? predicate = null) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                {
                    if ((string.IsNullOrEmpty(name) || (tChild is FrameworkElement fe && fe.Name == name)) &&
                        (predicate == null || predicate(tChild)))
                        return tChild;
                }

                var result = FindChild<T>(child, name, predicate);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static string[] GetServerStatus(string processID)
        {
            if (string.IsNullOrWhiteSpace(processID) || !int.TryParse(processID, out int pid))
                return ["OFF", "#FF5151"];

            try
            {
                var process = Process.GetProcessById(pid);
                if (!process.HasExited)
                    return ["ON", "#62FF59"];
            }
            catch (Exception) { }

            return ["OFF", "#FF5151"];
        }

        private static (double width, double height) GetButtonDimensions(double containerWidth, int buttonCount)
        {
            double margin = 2 * 25; // Left + Right margin per button
            double minButtonWidth = 250;
            double maxButtonWidth = 500;
            double maxButtonHeight = 250;

            double aspectRatio = 500 / 250 * 1.5;

            if (buttonCount <= 0) buttonCount = 1;

            double availableWidth = containerWidth - 20; // Account for WrapPanel margin
            double totalMargin = buttonCount * margin;
            double calculatedWidth = (availableWidth - totalMargin) / buttonCount;
            calculatedWidth = Math.Clamp(calculatedWidth, minButtonWidth, maxButtonWidth);

            double calculatedHeight = calculatedWidth / aspectRatio + 50;
            calculatedHeight = Math.Min(calculatedHeight, maxButtonHeight);

            return (calculatedWidth, calculatedHeight);
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