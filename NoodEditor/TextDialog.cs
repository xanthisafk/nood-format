using System.Windows;
using System.Windows.Controls;

public class TextDialog : Window
{
    public string Text { get; private set; }

    public TextDialog()
    {
        Width = 300;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = "Enter Text";

        var stack = new StackPanel { Margin = new Thickness(10) };

        var textBox = new System.Windows.Controls.TextBox { Margin = new Thickness(5) };
        var okButton = new System.Windows.Controls.Button { Content = "OK", Margin = new Thickness(5) };

        okButton.Click += (s, e) =>
        {
            Text = textBox.Text;
            DialogResult = true;
            Close();
        };

        stack.Children.Add(textBox);
        stack.Children.Add(okButton);
        Content = stack;
    }
}