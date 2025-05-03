using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ServerCardsCreator
{
    public class ServerCards : Window
    {
        private static readonly string? currentDirectory = Directory.GetCurrentDirectory();
        private static readonly string? rootFolder = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(currentDirectory)));

        private static readonly FontFamily ItimFont = rootFolder != null
            ? new FontFamily(System.IO.Path.Combine(rootFolder, "assets\\Fonts\\Itim\\#Itim"))
            : throw new InvalidOperationException("Root folder is not set.");
        private static readonly FontFamily JomhuriaFont = rootFolder != null
            ? new FontFamily(System.IO.Path.Combine(rootFolder, "assets\\Fonts\\Jomhuria\\#Jomhuria"))
            : throw new InvalidOperationException("Root folder is not set.");
        private static readonly Brush DarkBrush = (Brush)new BrushConverter().ConvertFrom("#262A32")!;
        private static readonly Brush HoverBrush = (Brush)new BrushConverter().ConvertFrom("#2F333D")!;
        private static readonly Brush StatusOnBrush = (Brush)new BrushConverter().ConvertFrom("#62FF59")!;
        private static readonly Brush StatusOffBrush = (Brush)new BrushConverter().ConvertFrom("#FF5151")!;
        private static readonly Brush StatusBackgroundBrush = (Brush)new BrushConverter().ConvertFrom("#3B414D")!;
        private static readonly Brush LightGrayBrush = (Brush)new BrushConverter().ConvertFrom("#D9D9D9")!;

        // Funcs for creating the created servers
        public static void UpdateButtonSizes(WrapPanel panel, double containerWidth, double shrinkIntensity = 2.0)
        {
            //double containerWidth = MainContent.ActualWidth;
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
                TextBlock? nameText = FindChildByName<TextBlock>(grid, "ServerNameVisualLabel");
                TextBlock? versionText = FindChildByName<TextBlock>(grid, "ServerVersionVisualisationLabel");
                TextBlock? playersText = FindChildByName<TextBlock>(grid, "ServerTotalPlayersVisualisationLabel");
                Grid? statusGrid = FindChildByName<Grid>(grid, "GridForON_OFF_Status");
                TextBlock? createServerTextBlock = FindChildByName<TextBlock>(grid, "CreateServerTextBlock");
                Ellipse? createServerCircle = FindChildByName<Ellipse>(grid, "CreateServerCircle");
                Rectangle? plusHorizontal = FindChildByName<Rectangle>(grid, "CreateServerPlusHorizontal");
                Rectangle? plusVertical = FindChildByName<Rectangle>(grid, "CreateServerPlusVertical");

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

        private static void ApplyButtonStyle(Button button, int cornerRadius)
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

        public static Button CreateStyledCreateButton(Thickness margin, int cornerRadius, Action onClick)
        {
            var button = new Button
            {
                Background = DarkBrush,
                Foreground = Brushes.White,
                FontSize = 25,
                Cursor = Cursors.Hand,
                Margin = margin,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                BorderThickness = new Thickness(0),
                ClipToBounds = true,
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

        public static Button CreateStyledButton(int cornerRadius, string name, string version, string totalPlayers, string processID, Thickness margin, Action onClick)
        {
            var button = new Button
            {
                Background = DarkBrush,
                Foreground = Brushes.White,
                FontSize = 25,
                Cursor = Cursors.Hand,
                Margin = margin,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                BorderThickness = new Thickness(0),
                ClipToBounds = true,
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

        private static Grid CreateButtonInnerGrid()
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
                Fill = LightGrayBrush // #D9D9D9
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
                Background = DarkBrush,
                CornerRadius = new CornerRadius(0, 25, 0, 0)
            };

            // Create hover style
            var borderStyle = new Style(typeof(Border));
            borderStyle.Setters.Add(new Setter(Border.BackgroundProperty, new BrushConverter().ConvertFrom("#262A32")));
            borderStyle.Triggers.Add(new Trigger
            {
                Property = IsMouseOverProperty,
                Value = true,
                Setters = {
                    new Setter(Border.BackgroundProperty, HoverBrush)
                }
            });
            topBorder.Style = borderStyle;

            var serverNameText = new TextBlock
            {
                Name = "ServerNameVisualLabel",
                Padding = new Thickness(10, 10, 15, 6),
                FontFamily = ItimFont,
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
                FontFamily = ItimFont,
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
                FontFamily = ItimFont,
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
                Background = StatusBackgroundBrush,
                CornerRadius = new CornerRadius(15)
            };

            var statusText = new TextBlock
            {
                FontFamily = JomhuriaFont,
                Margin = new Thickness(0, 7, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 50,
                Foreground = serverStatus[1] == "#62FF59" ? StatusOnBrush : StatusOffBrush,
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

        private static T? FindChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
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
    }
}