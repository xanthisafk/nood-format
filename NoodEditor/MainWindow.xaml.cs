using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NoodEditor
{
    public partial class MainWindow : Window
    {
        private NoodDocument currentDocument;
        private int currentPage = 0;
        private Canvas currentCanvas;
        private bool isEditing = true;

        private FrameworkElement selectedElement;
        private Border selectedBorder;

        public MainWindow()
        {
            InitializeComponent();
            InitializeNewDocument();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            InitializeFormatControls();
        }

        private void InitializeFormatControls()
        {
            boldButton.Click += (s, e) =>
            {
                if (selectedElement is TextBlock textBlock)
                    textBlock.FontWeight = ((ToggleButton)s).IsChecked == true ? FontWeights.Bold : FontWeights.Normal;
                UpdateDocument();
            };

            italicButton.Click += (s, e) =>
            {
                if (selectedElement is TextBlock textBlock)
                    textBlock.FontStyle = ((ToggleButton)s).IsChecked == true ? FontStyles.Italic : FontStyles.Normal;
                UpdateDocument();
            };

            underlineButton.Click += (s, e) =>
            {
                if (selectedElement is TextBlock textBlock)
                {
                    if (((ToggleButton)s).IsChecked == true)
                        textBlock.TextDecorations.Add(TextDecorations.Underline);
                    else
                        textBlock.TextDecorations.Remove(TextDecorations.Underline[0]);
                    UpdateDocument();
                }
            };

            strikeButton.Click += (s, e) =>
            {
                if (selectedElement is TextBlock textBlock)
                {
                    if (((ToggleButton)s).IsChecked == true)
                        textBlock.TextDecorations.Add(TextDecorations.Strikethrough);
                    else
                        textBlock.TextDecorations.Remove(TextDecorations.Strikethrough[0]);
                    UpdateDocument();
                }
            };

            fontSizeCombo.SelectionChanged += (s, e) =>
            {
                if (selectedElement is TextBlock textBlock && fontSizeCombo.SelectedItem is ComboBoxItem item)
                {
                    if (double.TryParse(item.Content.ToString(), out double size))
                        textBlock.FontSize = size;
                    UpdateDocument();
                }
            };
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Create a document for printing
                var fixedDoc = new FixedDocument();

                // Create a clean copy of the canvas for printing
                var printCanvas = new Canvas
                {
                    Width = currentCanvas.Width,
                    Height = currentCanvas.Height,
                    Background = System.Windows.Media.Brushes.White,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                };

                // Copy all elements
                foreach (UIElement element in currentCanvas.Children)
                {
                    if (element is Border border && border.Child is TextBlock textBlock)
                    {
                        var newTextBlock = new TextBlock
                        {
                            Text = textBlock.Text,
                            FontSize = textBlock.FontSize,
                            FontWeight = textBlock.FontWeight,
                            FontStyle = textBlock.FontStyle,
                            TextDecorations = textBlock.TextDecorations,
                            Foreground = textBlock.Foreground
                        };

                        var newBorder = new Border
                        {
                            Child = newTextBlock,
                            Background = System.Windows.Media.Brushes.Transparent,
                            Padding = border.Padding
                        };

                        Canvas.SetLeft(newBorder, Canvas.GetLeft(border));
                        Canvas.SetTop(newBorder, Canvas.GetTop(border));
                        printCanvas.Children.Add(newBorder);
                    }
                    else if (element is Grid grid && grid.Children.Count > 0 && grid.Children[0] is System.Windows.Controls.Image image)
                    {
                        var newImage = new System.Windows.Controls.Image
                        {
                            Source = image.Source,
                            Width = image.Width,
                            Height = image.Height,
                            Stretch = image.Stretch
                        };

                        Canvas.SetLeft(newImage, Canvas.GetLeft(grid));
                        Canvas.SetTop(newImage, Canvas.GetTop(grid));
                        printCanvas.Children.Add(newImage);
                    }
                }

                // Create a page content
                var pageContent = new PageContent();
                var fixedPage = new FixedPage();
                fixedPage.Width = currentCanvas.Width;
                fixedPage.Height = currentCanvas.Height;

                // Add the canvas to the page
                fixedPage.Children.Add(printCanvas);
                ((IAddChild)pageContent).AddChild(fixedPage);
                fixedDoc.Pages.Add(pageContent);

                // Print the document
                printDialog.PrintDocument(fixedDoc.DocumentPaginator, "NOOD Document");
            }
        }

        private void InitializeNewDocument()
        {
            currentDocument = new NoodDocument();
            currentDocument.AddPage(800, 1000); // Default page size
            SetupCanvas();
        }

        private void SetupCanvas()
        {
            mainScroll.Content = null;
            currentCanvas = new Canvas
            {
                Width = currentDocument.Pages[currentPage].Width,
                Height = currentDocument.Pages[currentPage].Height,
                Background = System.Windows.Media.Brushes.White
            };

            // Add existing elements if any
            foreach (var element in currentDocument.Pages[currentPage].Elements)
            {
                if (element.Type == PageElement.ElementType.Text)
                {
                    AddTextToCanvas(
                        Encoding.UTF8.GetString(element.Data),
                        element.X,
                        element.Y,
                        element.FontSize,
                        element.Color
                    );
                }
                else if (element.Type == PageElement.ElementType.Image)
                {
                    AddImageToCanvas(element.Data, element.X, element.Y);
                }
            }

            mainScroll.Content = currentCanvas;

            if (isEditing)
            {
                currentCanvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isEditing) return;

            var position = e.GetPosition(currentCanvas);
            var dialog = new ElementDialog();
            if (dialog.ShowDialog() == true)
            {
                if (dialog.IsText)
                {
                    AddText(dialog.TextContent, position.X, position.Y);
                }
                else
                {
                    AddImage(position.X, position.Y);
                }
            }
        }

        private void AddText(string text, double x, double y)
        {
            var textBlock = AddTextToCanvas(text, x, y, 12, 0x000000);
            currentDocument.AddText(currentPage, text, (float)x, (float)y);

            if (isEditing)
            {
                MakeElementDraggable(textBlock);
            }
        }

        private void UpdateDocument()
        {
            var page = currentDocument.Pages[currentPage];
            page.Elements.Clear();

            foreach (UIElement element in currentCanvas.Children)
            {
                if (element is Border border && border.Child is TextBlock textBlock)
                {
                    var x = (float)Canvas.GetLeft(border);
                    var y = (float)Canvas.GetTop(border);
                    var color = ((SolidColorBrush)textBlock.Foreground).Color;
                    var colorInt = (color.R << 16) | (color.G << 8) | color.B;

                    page.Elements.Add(new PageElement
                    {
                        Type = PageElement.ElementType.Text,
                        X = x,
                        Y = y,
                        FontSize = (float)textBlock.FontSize,
                        Color = colorInt,
                        IsBold = textBlock.FontWeight == FontWeights.Bold,
                        IsItalic = textBlock.FontStyle == FontStyles.Italic,
                        IsUnderline = textBlock.TextDecorations.Contains(TextDecorations.Underline[0]),
                        IsStrikethrough = textBlock.TextDecorations.Contains(TextDecorations.Strikethrough[0]),
                        Data = Encoding.UTF8.GetBytes(textBlock.Text)
                    });
                }
                else if (element is Grid grid && grid.Children[0] is System.Windows.Controls.Image image)
                {
                    var x = (float)Canvas.GetLeft(grid);
                    var y = (float)Canvas.GetTop(grid);

                    // Convert BitmapImage to byte array
                    byte[] imageData;
                    var bitmap = image.Source as BitmapImage;
                    using (var ms = new MemoryStream())
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        encoder.Save(ms);
                        imageData = ms.ToArray();
                    }

                    page.Elements.Add(new PageElement
                    {
                        Type = PageElement.ElementType.Image,
                        X = x,
                        Y = y,
                        Width = (float)image.Width,
                        Height = (float)image.Height,
                        Data = imageData
                    });
                }
            }
        }

        private TextBlock AddTextToCanvas(string text, double x, double y, float fontSize, int color)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(
                    (byte)((color >> 16) & 0xFF),
                    (byte)((color >> 8) & 0xFF),
                    (byte)(color & 0xFF)
                ))
            };

            // Apply text styling based on flags
            var element = currentDocument.Pages[currentPage].Elements
                .FirstOrDefault(e => e.Type == PageElement.ElementType.Text &&
                                    Math.Abs(e.X - x) < 0.1 &&
                                    Math.Abs(e.Y - y) < 0.1);

            if (element != null)
            {
                textBlock.FontWeight = element.IsBold ? FontWeights.Bold : FontWeights.Normal;
                textBlock.FontStyle = element.IsItalic ? FontStyles.Italic : FontStyles.Normal;
                if (element.IsUnderline)
                    textBlock.TextDecorations.Add(TextDecorations.Underline);
                if (element.IsStrikethrough)
                    textBlock.TextDecorations.Add(TextDecorations.Strikethrough);
            }

            var border = new Border
            {
                Child = textBlock,
                Background = System.Windows.Media.Brushes.Transparent,
                Padding = new Thickness(5)
            };

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);
            currentCanvas.Children.Add(border);

            if (isEditing)
            {
                border.MouseLeftButtonDown += (s, e) =>
                {
                    SelectElement(border, textBlock);
                    if (e.ClickCount == 1)
                    {
                        e.Handled = true;
                    }
                };
                MakeElementDraggable(border);
            }

            return textBlock;
        }

        private void SelectElement(FrameworkElement container, FrameworkElement element)
        {
            if (selectedBorder != null)
            {
                selectedBorder.BorderBrush = null;
                selectedBorder.BorderThickness = new Thickness(0);
            }

            selectedElement = element;
            if (container is Border border)
            {
                selectedBorder = border;
                border.BorderBrush = System.Windows.Media.Brushes.Blue;
                border.BorderThickness = new Thickness(1);

                // Update formatting controls
                if (element is TextBlock textBlock)
                {
                    boldButton.IsChecked = textBlock.FontWeight == FontWeights.Bold;
                    italicButton.IsChecked = textBlock.FontStyle == FontStyles.Italic;
                    underlineButton.IsChecked = textBlock.TextDecorations.Contains(TextDecorations.Underline[0]);
                    strikeButton.IsChecked = textBlock.TextDecorations.Contains(TextDecorations.Strikethrough[0]);
                    fontSizeCombo.Text = textBlock.FontSize.ToString();
                }
            }
        }

        private void AddImage(double x, double y)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg"
            };

            if (dialog.ShowDialog() == true)
            {
                var imageBytes = File.ReadAllBytes(dialog.FileName);
                var image = AddImageToCanvas(imageBytes, x, y);
                currentDocument.AddImage(currentPage, imageBytes, (float)x, (float)y);

                if (isEditing)
                {
                    MakeElementDraggable(image);
                }
            }
        }

        private System.Windows.Controls.Image AddImageToCanvas(byte[] imageData, double x, double y)
        {
            var image = new System.Windows.Controls.Image
            {
                Width = 200,
                Height = 200,
                Stretch = Stretch.Uniform
            };

            var bitmapImage = new BitmapImage();
            using (var ms = new MemoryStream(imageData))
            {
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }

            image.Source = bitmapImage;

            // Create container grid
            var container = new Grid();
            container.Children.Add(image);

            // Find corresponding element to get saved dimensions
            var element = currentDocument.Pages[currentPage].Elements
                .FirstOrDefault(e => e.Type == PageElement.ElementType.Image &&
                                    Math.Abs(e.X - x) < 0.1 &&
                                    Math.Abs(e.Y - y) < 0.1);

            if (element != null)
            {
                image.Width = element.Width;
                image.Height = element.Height;
            }

            // Add resize thumb
            var resizeThumb = new Thumb
            {
                Width = 10,
                Height = 10,
                Background = System.Windows.Media.Brushes.Blue,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Cursor = System.Windows.Input.Cursors.SizeNWSE
            };

            container.Children.Add(resizeThumb);

            Canvas.SetLeft(container, x);
            Canvas.SetTop(container, y);
            currentCanvas.Children.Add(container);

            if (isEditing)
            {
                MakeElementDraggable(container);
                resizeThumb.DragDelta += (s, e) =>
                {
                    image.Width = Math.Max(50, image.Width + e.HorizontalChange);
                    image.Height = Math.Max(50, image.Height + e.VerticalChange);
                };
            }

            return image;
        }

        private void MakeElementDraggable(FrameworkElement element)
        {
            System.Windows.Point? dragStart = null;

            element.MouseLeftButtonDown += (s, e) =>
            {
                dragStart = e.GetPosition(currentCanvas);
                element.CaptureMouse();
                e.Handled = true;
            };

            element.MouseMove += (s, e) =>
            {
                if (dragStart.HasValue)
                {
                    var position = e.GetPosition(currentCanvas);
                    var offset = position - dragStart.Value;
                    Canvas.SetLeft(element, Canvas.GetLeft(element) + offset.X);
                    Canvas.SetTop(element, Canvas.GetTop(element) + offset.Y);
                    dragStart = position;
                }
            };

            element.MouseLeftButtonUp += (s, e) =>
            {
                dragStart = null;
                element.ReleaseMouseCapture();
            };
        }

        private void SaveDocument_Click(object sender, RoutedEventArgs e)
        {
            UpdateDocument();
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "NOOD files (*.nood)|*.nood",
                DefaultExt = ".nood"
            };

            if (dialog.ShowDialog() == true)
            {
                currentDocument.SaveToFile(dialog.FileName);
            }
        }

        private void OpenDocument_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "NOOD files (*.nood)|*.nood"
            };

            if (dialog.ShowDialog() == true)
            {
                currentDocument = NoodDocument.LoadFromFile(dialog.FileName);
                currentPage = 0;
                SetupCanvas();
            }
        }

        private void ToggleMode_Click(object sender, RoutedEventArgs e)
        {
            isEditing = !isEditing;
            SetupCanvas();
            ((System.Windows.Controls.Button)sender).Content = isEditing ? "Switch to Reader" : "Switch to Editor";
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedElement is TextBlock textBlock)
            {
                var dialog = new ColorDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var color = dialog.Color;
                    var mcolor = new System.Windows.Media.Color
                    {
                        A = color.A,
                        R = color.R,
                        G = color.G,
                        B = color.B
                    };
                    textBlock.Foreground = new SolidColorBrush(mcolor);
                }
            }
        }
    }
}