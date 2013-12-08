using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro;
using MahApps.Metro.Controls;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace Vestbi
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : MetroWindow
    {

        Color linkBaseColor = Color.FromRgb(0x7d, 0x7d, 0x7a);

        public About()
        {
            InitializeComponent();

            var currentAccent = ThemeManager.DefaultAccents.First(x => x.Name == ProgramSettings.Current.Accent);
            var currentTheme = ProgramSettings.Current.Theme == "Light" ? Theme.Light : Theme.Dark;
            ThemeManager.ChangeTheme(this, currentAccent, currentTheme);

            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            TbVersion.Text = String.Format("VER {0}.{1}.{2}", ver.Major, ver.Minor, ver.Build);

            Resources["GrayBrush8"] = Resources["WindowTitleColorBrush"];
            Resources["GrayBrush5"] = Resources["HighlightBrush"];
            Resources["BlackBrush"] = Resources["WindowTitleColorBrush"];
            CloseButton.Foreground = Brushes.White;

            var lf = "license.txt";
            if (File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, lf)))
                LicenseBox.Text = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, lf));
        }

        private void CloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            UIElement button = (UIElement)sender;
            button.Effect = null;
        }

        private void CloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            UIElement button = (UIElement)sender;
            button.Effect = Helpers.MakePulsingShadow((Color)Resources["AccentColor"]);
        }

        private void CloseButton_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LinkMouseEnter(object sender, MouseEventArgs e)
        {
            TextBlock link = (TextBlock)sender;

            var anim = new ColorAnimation(linkBaseColor, (Color)Resources["AccentColor"], (Duration)TimeSpan.FromSeconds(0.5));
            anim.RepeatBehavior = new RepeatBehavior(777);
            anim.EasingFunction = new PowerEase() { EasingMode = EasingMode.EaseOut };
            anim.AutoReverse = true;
            link.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, anim, HandoffBehavior.SnapshotAndReplace);

            link.Effect = Helpers.MakePulsingShadow((Color)Resources["AccentColor"], false, 1);
        }

        private void LinkMouseLeave(object sender, MouseEventArgs e)
        {
            TextBlock link = (TextBlock)sender;
            link.Effect = null;
            link.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, null);
        }

        private void SourcesClick(object sender, MouseButtonEventArgs e)
        {
            TextBlock link = (TextBlock)sender;
            Process.Start(link.Text);
        }

        private void UpdatesClick(object sender, MouseButtonEventArgs e)
        {
            TextBlock link = (TextBlock)sender;
            Process.Start(link.Text);
        }

        private void MailToClick(object sender, MouseButtonEventArgs e)
        {
            var subject = WebUtility.UrlEncode("Vestbi");
            var body = WebUtility.UrlEncode("VVh4+'s uP m4n? ch3ck d1s 0u+!").Replace("+", "%20");
            Process.Start(String.Format("mailto:pavelluden@gmail.com?Subject={0}&body={1}", subject, body));
        }
    }
}
