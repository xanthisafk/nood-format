using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows;

public class ElementDialog : Window
{
    public bool IsText { get; private set; }
    public string TextContent { get; private set; }

    public ElementDialog()
    {
        Width = 300;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = "Add Element";

        var stack = new StackPanel { Margin = new Thickness(10) };

        var textButton = new System.Windows.Controls.Button { Content = "Add Text", Margin = new Thickness(5) };
        textButton.Click += (s, e) =>
        {
            var textDialog = new TextDialog();
            if (textDialog.ShowDialog() == true)
            {
                IsText = true;
                TextContent = textDialog.Text;
                DialogResult = true;
            }
            Close();
        };

        var imageButton = new System.Windows.Controls.Button { Content = "Add Image", Margin = new Thickness(5) };
        imageButton.Click += (s, e) =>
        {
            IsText = false;
            DialogResult = true;
            Close();
        };

        stack.Children.Add(textButton);
        stack.Children.Add(imageButton);
        Content = stack;
    }
}