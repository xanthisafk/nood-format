using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NoodEditor
{
    public class AboutPage: Window
    {
        public AboutPage()
        {
            var textblock = new TextBlock
            {
                Text = "NOOD (Noodles' Optimized Object Document) format.\nMade by Abhinav."
            };
            var btn = new System.Windows.Controls.Button
            {
                Content = "GitHub",
            };
            var stack = new StackPanel();
            
        }
    }
}
