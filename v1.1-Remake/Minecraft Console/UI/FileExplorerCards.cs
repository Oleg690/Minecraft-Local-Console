using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;

namespace Minecraft_Console.UI
{
    public class FileExplorerCards : Window
    {
        // === Constants ===
        private static readonly Color NormalItemColor = Color.FromRgb(38, 42, 50);
        private static readonly Color HoverItemColor = Color.FromRgb(50, 55, 65);

        // === Entry Point ===
        public static void LoadFiles(string path, Grid grid, StackPanel panel)
        {
            if (path[^1] == '\\') path = path[..^1];
            if (MainWindow.CurrentPath == path) return;

            ResetExplorerPopup();
            MainWindow.CurrentPath = path;

            var filesAndFolders = ServerFileExplorer.GetFoldersAndFiles(MainWindow.CurrentPath);
            CreateExplorerItems(grid, filesAndFolders, panel);
            DisplayPathComponents(MainWindow.CurrentPath, grid, panel);
        }

        // === Explorer Rendering ===
        public static void DisplayPathComponents(string fullPath, Grid grid, StackPanel panel)
        {
            panel.Children.Clear(); // clear previous buttons
            List<string[][]> components = ServerFileExplorer.GetStructuredPathComponents(fullPath);

            panel.Children.Add(new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 25,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(124, 124, 124)),
                Text = "/",
                RenderTransform = new TranslateTransform(0, -2)
            });

            for (int i = 0; i < components.Count; i++)
            {
                string fullDirPath = components[i][0][0]; // full path with trailing '\'
                string folderName = components[i][1][0];  // name of the current folder

                var btn = new Button
                {
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Content = folderName,
                    Tag = fullDirPath,
                    Template = (ControlTemplate)panel.FindResource("PathButtonTemplate"),
                };

                btn.Click += (s, e) =>
                {
                    string? pathToLoad = ((Button)s).Tag?.ToString();
                    if (pathToLoad != null)
                    {
                        LoadFiles(pathToLoad, grid, panel);
                    }
                };

                panel.Children.Add(btn);

                if (i < components.Count - 1)
                {
                    panel.Children.Add(new TextBlock
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 25,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(124, 124, 124)),
                        Text = "/",
                        RenderTransform = new TranslateTransform(0, -2)
                    });
                }
            }
        }

        public static void CreateExplorerItems(Grid parentGrid, List<List<string>> items, StackPanel panel)
        {
            ResetExplorerPopup();
            parentGrid.Children.Clear();
            parentGrid.RowDefinitions.Clear();

            ScrollViewer scrollViewer = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0),
                Padding = new Thickness(0)
            };

            Grid internalGrid = new();
            scrollViewer.Content = internalGrid;
            parentGrid.Children.Add(scrollViewer);

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.Count < 2) continue;

                string itemName = item[0];
                string itemType = item[1].ToLower();
                string path = item[2];
                string fileSize = item.Count > 3 ? item[3] : "";
                string lastOpened = item.Count > 4 ? item[4] : "";

                internalGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Border explorerItem = new()
                {
                    Name = $"itemName",
                    Background = new SolidColorBrush(Color.FromRgb(38, 42, 50)),
                    CornerRadius = new CornerRadius(10),
                    Height = 50,
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(5),
                    Tag = path,
                    Opacity = 0
                };

                explorerItem.MouseEnter += ItemHoverOn;
                explorerItem.MouseLeave += ItemHoverOff;

                if (itemType == "file")
                    explorerItem.MouseLeftButtonDown += (sender, e) => OpenFile(itemName, parentGrid, panel);
                else if (itemType == "folder")
                    explorerItem.MouseLeftButtonDown += (sender, e) => OpenFolder(itemName, parentGrid, panel);

                Grid.SetRow(explorerItem, i);
                Grid.SetColumnSpan(explorerItem, 1);

                string ExplorerItemGrid =
                $@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width='Auto' />
                        <ColumnDefinition Width='Auto' />
                        <ColumnDefinition Width='*' />
                        <ColumnDefinition Width='Auto' />
                        <ColumnDefinition Width='Auto' />
                    </Grid.ColumnDefinitions>
                
                    <CheckBox x:Name='itemCheckBox' Grid.Column='0' Margin='10' VerticalAlignment='Center' Cursor='Hand'>
                        <CheckBox.LayoutTransform>
                            <ScaleTransform ScaleX='1.5' ScaleY='1.5'/>
                        </CheckBox.LayoutTransform>
                        <CheckBox.Template>
                            <ControlTemplate TargetType='{{x:Type CheckBox}}'>
                                <StackPanel>
                                    <Border x:Name='Border' Width='20' Height='20' Background='Transparent' BorderBrush='White' BorderThickness='2' CornerRadius='2'>
                                        <Path x:Name='CheckMark' Data='M 0 6 L 4 10 L 12 2' Stretch='Uniform' Stroke='White' StrokeThickness='2.2' Visibility='Collapsed'/>
                                    </Border>
                                    <ContentPresenter Margin='5,0,0,0' HorizontalAlignment='Left' VerticalAlignment='Center'/>
                                </StackPanel>
                
                                <ControlTemplate.Triggers>
                                    <Trigger Property='IsChecked' Value='True'>
                                        <Setter TargetName='CheckMark' Property='Visibility' Value='Visible'/>
                                    </Trigger>
                
                                    <Trigger Property='IsMouseOver' Value='True'>
                                        <Trigger.EnterActions>
                                            <BeginStoryboard>
                                                <Storyboard>
                                                    <ColorAnimation Storyboard.TargetName='Border' Storyboard.TargetProperty='BorderBrush.Color' To='#D0D0D0' Duration='0:0:0.2'/>
                                                    <ColorAnimation Storyboard.TargetName='CheckMark' Storyboard.TargetProperty='Stroke.Color' To='#D0D0D0' Duration='0:0:0.2'/>
                                                </Storyboard>
                                            </BeginStoryboard>
                                        </Trigger.EnterActions>
                
                                        <Trigger.ExitActions>
                                            <BeginStoryboard>
                                                <Storyboard>
                                                    <ColorAnimation Storyboard.TargetName='Border' Storyboard.TargetProperty='BorderBrush.Color' To='White' Duration='0:0:0.2'/>
                                                    <ColorAnimation Storyboard.TargetName='CheckMark' Storyboard.TargetProperty='Stroke.Color' To='White' Duration='0:0:0.2'/>
                                                </Storyboard>
                                            </BeginStoryboard>
                                        </Trigger.ExitActions>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </CheckBox.Template>
                    </CheckBox>
                
                    <Image Grid.Column='1' Width='30' Height='30' Margin='5' VerticalAlignment='Center' Source='pack://application:,,,/assets/icons/folder_file/{itemType}.png'/>
                
                    <TextBlock x:Name='ItemNameTextBlock' Grid.Column='2' Margin='5,4,10,0' VerticalAlignment='Center' FontSize='25' Foreground='White' Text='{itemName}'/>
                
                    <Grid
                        Grid.Column='3'
                        Width='350'
                        Margin='0,0,20,0'
                        HorizontalAlignment='Stretch'
                        VerticalAlignment='Center'>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width='Auto' />
                            <ColumnDefinition Width='*' />
                        </Grid.ColumnDefinitions>

                        <TextBlock
                            x:Name='ItemMemoryTextBlock'
                            Grid.Column='0'
                            Margin='5,6,10,0'
                            HorizontalAlignment='Left'
                            VerticalAlignment='Center'
                            FontSize='20'
                            Foreground='#a5a5a5'
                            Text='{fileSize}' />

                        <TextBlock
                            x:Name='ItemLastTimeOpenedTextBlock'
                            Grid.Column='1'
                            Margin='5,6,10,0'
                            HorizontalAlignment='Right'
                            VerticalAlignment='Center'
                            FontSize='20'
                            Foreground='#a5a5a5'
                            Text='{lastOpened}' />
                    </Grid>

                    <ComboBox x:Name='FilesDropdown'
                              Grid.Column='4'
                              Width='40'
                              Margin='10'
                              Background='Transparent'
                              BorderThickness='0'
                              Cursor='Hand'
                              FontSize='14'
                              Foreground='White'
                              IsEditable='False'
                              ScrollViewer.HorizontalScrollBarVisibility='Auto'
                              ScrollViewer.VerticalScrollBarVisibility='Auto'>
                
                        <ComboBox.Resources>
                            <Style TargetType='ComboBoxItem'>
                                <Setter Property='Background' Value='#FF333333'/>
                                <Setter Property='Foreground' Value='White'/>
                                <Setter Property='BorderThickness' Value='0'/>
                                <Setter Property='Template'>
                                    <Setter.Value>
                                        <ControlTemplate TargetType='ComboBoxItem'>
                                            <Border x:Name='Bd' Padding='8,4' Background='{{TemplateBinding Background}}' BorderBrush='{{TemplateBinding BorderBrush}}' BorderThickness='{{TemplateBinding BorderThickness}}' CornerRadius='4'>
                                                <ContentPresenter/>
                                            </Border>
                                            <ControlTemplate.Triggers>
                                                <Trigger Property='IsHighlighted' Value='true'>
                                                    <Setter TargetName='Bd' Property='Background' Value='#FF555555'/>
                                                </Trigger>
                                                <Trigger Property='IsMouseOver' Value='true'>
                                                    <Setter TargetName='Bd' Property='Background' Value='#FF444444'/>
                                                    <Setter TargetName='Bd' Property='CornerRadius' Value='4'/>
                                                </Trigger>
                                            </ControlTemplate.Triggers>
                                        </ControlTemplate>
                                    </Setter.Value>
                                </Setter>
                            </Style>
                        </ComboBox.Resources>
                
                        <ComboBox.Template>
                            <ControlTemplate TargetType='ComboBox'>
                                <Grid>
                                    <ToggleButton x:Name='ToggleButton'
                                                  HorizontalAlignment='Stretch'
                                                  Background='Transparent'
                                                  BorderBrush='White'
                                                  BorderThickness='3'
                                                  ClickMode='Press'
                                                  Focusable='false'
                                                  IsChecked='{{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={{RelativeSource TemplatedParent}}}}'>
                                        <ToggleButton.Template>
                                            <ControlTemplate TargetType='ToggleButton'>
                                                <Border x:Name='Border'
                                                        Background='{{TemplateBinding Background}}'
                                                        BorderBrush='{{TemplateBinding BorderBrush}}'
                                                        BorderThickness='{{TemplateBinding BorderThickness}}'
                                                        CornerRadius='5'>
                                                    <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
                                                </Border>
                                                <ControlTemplate.Triggers>
                                                    <Trigger Property='IsMouseOver' Value='True'>
                                                        <Setter TargetName='Border' Property='Background' Value='#1AFFFFFF'/>
                                                    </Trigger>
                                                </ControlTemplate.Triggers>
                                            </ControlTemplate>
                                        </ToggleButton.Template>
                                        <TextBlock Margin='0,-12,0,0'
                                                   HorizontalAlignment='Center'
                                                   VerticalAlignment='Center'
                                                   FontFamily='Sans Serif Collection'
                                                   FontSize='30'
                                                   Foreground='White'
                                                   Text='...'
                                                   TextAlignment='Center'/>
                                    </ToggleButton>
                                    <Popup x:Name='Popup'
                                           AllowsTransparency='True'
                                           Focusable='False'
                                           IsOpen='{{TemplateBinding IsDropDownOpen}}'
                                           Placement='Bottom'
                                           PopupAnimation='Slide'>
                                        <Border MinWidth='{{Binding ActualWidth, RelativeSource={{RelativeSource TemplatedParent}}}}'
                                                Background='#FF333333'
                                                BorderBrush='#FF555555'
                                                BorderThickness='1'
                                                CornerRadius='4'>
                                            <ScrollViewer>
                                                <ItemsPresenter/>
                                            </ScrollViewer>
                                        </Border>
                                    </Popup>
                                </Grid>
                            </ControlTemplate>
                        </ComboBox.Template>
                
                        <ComboBox.Style>
                            <Style TargetType='ComboBox'>
                                <Setter Property='OverridesDefaultStyle' Value='True'/>
                            </Style>
                        </ComboBox.Style>
                
                        <ComboBoxItem Content='Rename'
                                      Cursor='Hand'
                                      FontFamily='pack://application:,,,/assets/Fonts/Istok Web/#Istok Web'
                                      FontSize='18'/>
                        <ComboBoxItem Content='Move'
                                      Cursor='Hand'
                                      FontFamily='pack://application:,,,/assets/Fonts/Istok Web/#Istok Web'
                                      FontSize='18'/>
                        <ComboBoxItem Content='Archive'
                                      Cursor='Hand'
                                      FontFamily='pack://application:,,,/assets/Fonts/Istok Web/#Istok Web'
                                      FontSize='18'/>
                        <ComboBoxItem Content='Delete'
                                      Cursor='Hand'
                                      FontFamily='pack://application:,,,/assets/Fonts/Istok Web/#Istok Web'
                                      FontSize='18'/>
                    </ComboBox>
                </Grid>";

                var stringReader = new StringReader(ExplorerItemGrid);
                var xmlReader = XmlReader.Create(stringReader);
                var element = (UIElement)XamlReader.Load(xmlReader);

                if (element is Grid grid)
                {
                    var comboBox = grid.Children.OfType<ComboBox>().FirstOrDefault(cb => cb.Name == "FilesDropdown");
                    if (comboBox != null)
                        comboBox.SelectionChanged += FilesDropdown_SelectionChanged;

                    var checkBox = grid.Children.OfType<CheckBox>().FirstOrDefault(cb => cb.Name == "itemCheckBox");
                    if (checkBox != null)
                    {
                        checkBox.Checked += ExplorerCheckBox_Checked;
                        checkBox.Unchecked += ExplorerCheckBox_Unchecked;
                    }
                }

                explorerItem.Child = element;
                internalGrid.Children.Add(explorerItem);

                // --- Animation Logic ---
                DoubleAnimation fadeInAnimation = new()
                {
                    To = 1, // Fade to full opacity
                    Duration = TimeSpan.FromSeconds(0.5), // Duration of the fade-in
                    BeginTime = TimeSpan.FromSeconds(i * 0.025) // Stagger the start time for each item
                };

                Storyboard storyboard = new();
                Storyboard.SetTarget(fadeInAnimation, explorerItem);
                Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(fadeInAnimation);
                storyboard.Begin();
            }

            if (!string.IsNullOrEmpty(MainWindow.CurrentPath))
                DisplayPathComponents(MainWindow.CurrentPath, parentGrid, panel);
            else
                MessageBox.Show("CurrentPath is null or empty. Unable to display path components.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // === Item Events ===
        private static void ItemHoverOn(object sender, MouseEventArgs e)
        {
            if (sender is not Border border || IsHoveringOverType<CheckBox>(e, border))
                return;

            border.Background = new SolidColorBrush(HoverItemColor);
        }

        private static void ItemHoverOff(object sender, MouseEventArgs e)
        {
            if (sender is not Border border)
                return;

            border.Background = new SolidColorBrush(NormalItemColor);
        }

        private static bool IsHoveringOverType<T>(MouseEventArgs e, Border border) where T : DependencyObject
        {
            var position = e.GetPosition(border);
            var hit = border.InputHitTest(position) as DependencyObject;

            while (hit != null)
            {
                if (hit is T)
                    return true;

                hit = VisualTreeHelper.GetParent(hit);
            }

            return false;
        }

        // === File/Folder Opening ===
        private static void OpenFolder(string fileName, Grid parentGrid, StackPanel panel)
        {
            if (string.IsNullOrEmpty(MainWindow.CurrentPath))
            {
                MessageBox.Show("Current path is null.");
                return;
            }

            ResetExplorerPopup();

            MainWindow.CurrentPath = Path.Combine(MainWindow.CurrentPath, fileName);

            List<List<string>> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(MainWindow.CurrentPath);
            CreateExplorerItems(parentGrid, Files_Folders, panel);
        }

        private static void OpenFile(string fileName, Grid parentGrid, StackPanel panel)
        {
            if (MainWindow.CurrentPath == null)
            {
                MessageBox.Show("CurrentPath not is not set!");
                return;
            }

            ResetExplorerPopup();

            string combinedPath = Path.Combine(MainWindow.CurrentPath, fileName);

            string[] supportedExtensions = [
                ".txt", ".cs", ".js", ".html", ".css", ".json", ".xml", ".log",
                ".py", ".java", ".cpp", ".c", ".h", ".php", ".rb", ".go", ".swift",
                ".ts", ".jsx", ".tsx", ".vue", ".sh", ".bat", ".ini", ".config",
                ".yaml", ".yml", ".md", ".sql", ".r", ".pl", ".dart", ".kt",
                ".asm", ".pas", ".vb", ".lua", ".groovy", ".fs", ".rs", ".scala",
                ".diff", ".patch", ".cmake", ".dockerfile", ".gitignore", ".env", ".properties",
                // Minecraft server files specific extensions
                ".yml", ".json", ".properties", ".conf", ".txt", ".log", ".toml"
            ];
            string fileExtension = Path.GetExtension(combinedPath).ToLower();

            if (!supportedExtensions.Contains(fileExtension))
            {
                MessageBox.Show($"File type '{fileExtension}' is not supported for viewing.", "Unsupported File Type", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string? content = ServerFileExplorer.ReadFromFile(combinedPath) ?? "";
            if (content == null)
            {
                return;
            }

            parentGrid.Children.Clear();

            string xaml =
                  $@"<Border 
                             xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                             x:Name='EditorBorder'
                             Background='#222428'
                             BorderThickness='0'
                             CornerRadius='10'
                             Margin='5'
                             Padding='10'
                             Opacity='0'>
                        <Grid>
                             <Grid.RowDefinitions>
                               <RowDefinition Height='Auto' />
                               <RowDefinition Height='*' />
                             </Grid.RowDefinitions>
                             <Grid.ColumnDefinitions>
                               <ColumnDefinition Width='*' />
                               <ColumnDefinition Width='Auto' />
                             </Grid.ColumnDefinitions>
                             
                             <TextBlock Text='{fileName}'
                                        FontSize='22'
                                        FontWeight='Bold'
                                        Foreground='White'
                                        VerticalAlignment='Center'
                                        HorizontalAlignment='Left'
                                        Margin='0,0,10,10'
                                        Grid.Row='0'
                                        Grid.Column='0' />
                             
                             <Button x:Name='ExplorerSaveButton'
                                     Content='Save'
                                     FontSize='14'
                                     Padding='10,5'
                                     HorizontalAlignment='Right'
                                     VerticalAlignment='Top'
                                     Background='#3a8ff1'
                                     Foreground='White'
                                     BorderBrush='Transparent'
                                     Cursor='Hand'
                                     Margin='0,0,0,10'
                                     Grid.Row='0'
                                     Grid.Column='1' />
                            
                            <ScrollViewer x:Name='EditorScrollViewer'
                                          Grid.Row='1' 
                                          Grid.Column='0' 
                                          Grid.ColumnSpan='2'
                                          VerticalScrollBarVisibility='Auto'
                                          HorizontalScrollBarVisibility='Auto'
                                          MaxHeight='{parentGrid.ActualHeight - 72}'>
                                          <TextBox
                                              x:Name='LogTextBox'
                                              Margin='0'
                                              Padding='10'
                                              AcceptsReturn='True'
                                              Background='#333333'
                                              BorderBrush='Transparent'
                                              BorderThickness='0'
                                              FocusVisualStyle='{{x:Null}}'
                                              FontSize='16'
                                              Foreground='White'
                                              TextWrapping='Wrap'>
                                          <TextBox.Style>
                                              <Style BasedOn='{{StaticResource {{x:Type TextBox}}}}' TargetType='TextBox'>
                                                  <Setter Property='Template'>
                                                      <Setter.Value>
                                                          <ControlTemplate TargetType='TextBox'>
                                                              <ScrollViewer x:Name='PART_ContentHost' Background='{{TemplateBinding Background}}' />
                                                          </ControlTemplate>
                                                      </Setter.Value>
                                                  </Setter>
                                              </Style>
                                          </TextBox.Style>
                                        </TextBox>
                            </ScrollViewer>
                        </Grid>
                      </Border>";

            var stringReader = new StringReader(xaml);
            var xmlReader = XmlReader.Create(stringReader);
            var element = (UIElement)XamlReader.Load(xmlReader);

            parentGrid.Children.Add(element);
            parentGrid.SizeChanged += UpdateScrollViewer;

            if (element is Border editorBorder)
            {
                DoubleAnimation fadeInAnimation = new()
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.2)), // Adjust duration as needed
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } // Optional: Add an easing function
                };

                Storyboard.SetTarget(fadeInAnimation, editorBorder);
                Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(OpacityProperty));

                Storyboard fadeInStoryboard = new();
                fadeInStoryboard.Children.Add(fadeInAnimation);
                fadeInStoryboard.Begin();
            }

            MainWindow.CurrentPath = combinedPath;

            if (!string.IsNullOrEmpty(MainWindow.CurrentPath))
            {
                DisplayPathComponents(MainWindow.CurrentPath, parentGrid, panel);
            }
            else
            {
                MessageBox.Show("CurrentPath is null or empty. Unable to display path components.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Correctly cast the object to TextBox
            if (FindLogicalChildByName<TextBox>(element, "LogTextBox") is not TextBox logTextBox)
            {
                CodeLogger.ConsoleLog("LogTextBox not found in XAML.");
                return;
            }

            logTextBox.Text = content;

            if (FindLogicalChildByName<Button>(element, "ExplorerSaveButton") is not Button saveButton)
            {
                CodeLogger.ConsoleLog("ExplorerSaveButton not found in XAML.");
                return;
            }

            saveButton.Click += (s, e) =>
            {
                try
                {
                    ServerFileExplorer.WriteToFile(MainWindow.CurrentPath, logTextBox.Text);
                    MessageBox.Show("Saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        private static void UpdateScrollViewer(object sender, SizeChangedEventArgs e)
        {
            if (sender is Grid parentGrid && parentGrid.Children.Count > 0)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(parentGrid);
                if (scrollViewer != null)
                {
                    scrollViewer.MaxHeight = parentGrid.ActualHeight - 72; // Adjust based on your layout
                }
            }
        }

        // === Dropdown & Context Menu ===
        private static void FilesDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox ||
                comboBox.SelectedItem is not ComboBoxItem selectedItem ||
                selectedItem.Content == null)
            {
                return;
            }

            string? selectedAction = selectedItem.Content.ToString();

            // Execute the appropriate action based on selection
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                switch (selectedAction)
                {
                    case "Rename":
                        ExecuteFileAction(comboBox, "Rename", mainWindow.PopupHost);
                        break;
                    case "Move":
                        ExecuteFileAction(comboBox, "Move", mainWindow.PopupHost);
                        break;
                    case "Archive":
                        ExecuteFileAction(comboBox, "Archive", mainWindow.PopupHost);
                        break;
                    case "Delete":
                        ExecuteFileAction(comboBox, "Delete", mainWindow.PopupHost);
                        break;
                }

                comboBox.SelectedItem = null;
                comboBox.IsDropDownOpen = false;

                List<List<string>> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(MainWindow.CurrentPath);
                CreateExplorerItems(mainWindow.ExplorerParent, Files_Folders, mainWindow.pathContainer);
            }
        }

        private async static void ExecuteFileAction(ComboBox comboBox, string action, Grid PopupHost)
        {
            if (MainWindow.CurrentPath == null)
            {
                MessageBox.Show("CurrentPath not is not set!");
                CodeLogger.ConsoleLog("CurrentPath not is not set!");
                return;
            }

            if (MainWindow.userDataPath == null)
            {
                MessageBox.Show("userDataPath not is not set!");
                CodeLogger.ConsoleLog("userDataPath not is not set!");
                return;
            }

            string itemName = GetContextName(comboBox);
            string itemPath = Path.Combine(MainWindow.CurrentPath, itemName);

            if (action == "Rename")
            {
                MessageBox.Show($"{action} clicked for: {itemPath}");
            }
            else if (action == "Move")
            {
                MessageBox.Show($"{action} clicked for: {itemPath}");
            }
            else if (action == "Archive")
            {
                try
                {
                    string downloadsPath = JsonHelper.GetOrSetValue(MainWindow.userDataPath, "archivePath")?.ToString() ?? string.Empty;
                    List<string> path = [itemPath];

                    string[] archivingStatus = await Task.Run(() => MainWindow.AchiveExplorerItems(path, downloadsPath));

                    PopupWindow.CreateStatusPopup(archivingStatus[0], archivingStatus[1], PopupHost);
                }
                catch (Exception ex)
                {
                    PopupWindow.CreateStatusPopup("Error", $"Error archiving {Path.GetFileName(itemPath)}. {ex}", PopupHost);
                }
            }
            else if (action == "Delete")
            {
                try
                {
                    string status = "";
                    string message = $"";

                    if (File.Exists(itemPath))
                    {
                        File.Delete(itemPath);
                        status = "Success";
                        message = $"File {itemName} deleted successfully.";
                    }
                    else if (Directory.Exists(itemPath))
                    {
                        Directory.Delete(itemPath, true);
                        status = "Success";
                        message = $"Directory {itemName} deleted successfully.";
                    }
                    else
                    {
                        status = "Error";
                        message = $"Item not found: {itemName}";
                    }

                    PopupWindow.CreateStatusPopup(status, message, PopupHost);
                }
                catch (UnauthorizedAccessException)
                {
                    PopupWindow.CreateStatusPopup("Error", $"Access denied for deletion.", PopupHost);
                }
                catch (IOException ex) when (ex.Message.Contains("being used by another process"))
                {
                    PopupWindow.CreateStatusPopup("Error", $"Item in use.", PopupHost);
                }
                catch (Exception ex)
                {
                    PopupWindow.CreateStatusPopup("Error", $"Error deleting {Path.GetFileName(itemPath)}. {ex}", PopupHost);
                }
            }
        }

        private static string GetContextName(ComboBox comboBox)
        {
            var parentGrid = FindVisualParent<Grid>(comboBox);
            if (parentGrid == null) return "Unknown item";

            // Find the TextBlock in column 2 (assuming it contains the item name)
            var textBlock = parentGrid.Children
                .OfType<TextBlock>()
                .FirstOrDefault(tb => Grid.GetColumn(tb) == 2);

            return textBlock?.Text ?? "Unknown item";
        }

        // === Selection/Checkbox Popup ===
        private static void ExplorerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                object[] selectedItems = ShowSelectedItems(mainWindow.ExplorerParent);
                mainWindow.ExplorerPopupText.Text = selectedItems[0].ToString();

                mainWindow.ExplorerPopupStatus = true;
                ExplorerStatusMove(mainWindow.ExplorerPopupStatus, mainWindow.SelectionPopup);
            }
        }

        private static void ExplorerCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                object[] selectedItems = ShowSelectedItems(mainWindow.ExplorerParent);
                mainWindow.ExplorerPopupText.Text = selectedItems[0].ToString();

                _ = int.TryParse(selectedItems[1].ToString(), out int counter);
                mainWindow.ExplorerPopupStatus = counter > 0;
                ExplorerStatusMove(mainWindow.ExplorerPopupStatus, mainWindow.SelectionPopup);
            }
        }

        private static void ExplorerStatusMove(bool status, Border Popup)
        {
            if (status)
            {
                ShowSelectionPopup(Popup);
            }
            else
            {
                HideSelectionPopup(Popup);
            }
        }

        private static void ShowSelectionPopup(Border SelectionPopup)
        {
            SelectionPopup.Visibility = Visibility.Visible;

            // Get the TranslateTransform from the border
            if (SelectionPopup.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                SelectionPopup.RenderTransform = transform;
            }

            // Create the animation
            DoubleAnimation slideIn = new()
            {
                To = 0,       // End Y position (visible)
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } // Smooth glide
            };

            transform.BeginAnimation(TranslateTransform.YProperty, slideIn);
        }

        private static void HideSelectionPopup(Border SelectionPopup)
        {
            if (SelectionPopup.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                SelectionPopup.RenderTransform = transform;
            }

            DoubleAnimation slideOut = new()
            {
                To = -160,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            slideOut.Completed += (s, e) => SelectionPopup.Visibility = Visibility.Hidden;

            transform.BeginAnimation(TranslateTransform.YProperty, slideOut);
        }

        public static object[] ShowSelectedItems(Grid ExplorerParent)
        {
            var selectedItems = GetSelectedItems(ExplorerParent);
            var message = selectedItems.Count == 0
                ? "No items selected"
                : $"Selected items: {selectedItems.Count}";

            return [message, selectedItems.Count.ToString(), selectedItems];
        }

        private static List<string> GetSelectedItems(Grid explorerParent)
        {
            var selectedItems = new List<string>();

            // Recursively get all Border elements inside explorerParent
            var allBorders = FindVisualChildren<Border>(explorerParent);

            foreach (var border in allBorders)
            {
                var checkBox = FindVisualChild<CheckBox>(border);
                if (checkBox != null && checkBox.IsChecked == true && border.Tag is string itemName)
                {
                    selectedItems.Add(itemName);
                }
            }

            return selectedItems;
        }

        public static void ResetExplorerPopup()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.SelectAllCheckBox.IsChecked = false;
                mainWindow.ExplorerPopupStatus = false;

                mainWindow.ExplorerPopupText.Text = "No selected items";
                HideSelectionPopup(mainWindow.SelectionPopup);
            }
        }

        // === Visual Tree Helpers ===
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;

                var foundChild = FindVisualChild<T>(child);
                if (foundChild != null)
                    return foundChild;
            }
            return null;
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

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        static T? FindLogicalChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            foreach (object child in LogicalTreeHelper.GetChildren(parent))
            {
                if (child is FrameworkElement fe)
                {
                    if (fe is T typedChild && fe.Name == name)
                        return typedChild;

                    var foundChild = FindLogicalChildByName<T>(fe, name);
                    if (foundChild != null)
                        return foundChild;
                }
            }
            return null;
        }
    }
}