using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace NoodEditor
{
    public class AboutPage: Window
    {
        public AboutPage()
        {
            Title = "About .nood";
            Width = 300;
            Height = 250;
            var textblock = new TextBlock
            {
                Text = "NOOD (Noodles' Optimized Object Document) format.\nMade by Abhinav.",
                TextWrapping = TextWrapping.Wrap,
            };
            var btn = new System.Windows.Controls.Button
            {
                Content = "GitHub",
            };
            btn.Click += (s, e) => {
                var proc = new System.Diagnostics.ProcessStartInfo("https://github.com/xanthisafk/nood-format")
                { 
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(proc);
            };
            var clsBtn = new System.Windows.Controls.Button
            {
                Content = "Close"
            };
            var img = new System.Windows.Controls.Image{
                Source = new BitmapImage(new Uri("pack://application:,,,/Resources/logo.png")),
                Width = 100,
                Height = 100,
            };
            btn.Click += (s, e) => this.Close();
            var stack = new StackPanel();
            stack.Children.Add(img);
            stack.Children.Add(textblock);
            stack.Children.Add(btn);
            stack.Children.Add(clsBtn);
            var scroller = new ScrollViewer();
            scroller.Content = stack;
            Content = scroller;
        }
    }
}
