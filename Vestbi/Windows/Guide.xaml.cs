using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro;
using MahApps.Metro.Controls;

namespace Vestbi
{
    /// <summary>
    /// Interaction logic for Guide.xaml
    /// </summary>
    public partial class Guide : MetroWindow
    {
        int curSlide = 0;
        int slidesCount = 4;

        public static BitmapImage LoadBitmapFromResource(string pathInApplication, Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            if (pathInApplication[0] == '/')
            {
                pathInApplication = pathInApplication.Substring(1);
            }
            return new BitmapImage(new Uri(@"pack://application:,,,/Vestbi;component/" + pathInApplication, UriKind.Absolute));
        }

        public Guide()
        {
            InitializeComponent();

            var currentAccent = ThemeManager.DefaultAccents.First(x => x.Name == ProgramSettings.Current.Accent);
            var currentTheme = ProgramSettings.Current.Theme == "Light" ? Theme.Light : Theme.Dark;
            ThemeManager.ChangeTheme(this, currentAccent, currentTheme);

            Resources["GrayBrush8"] = Resources["WindowTitleColorBrush"];
            Resources["GrayBrush5"] = Resources["HighlightBrush"];
            Resources["BlackBrush"] = Resources["WindowTitleColorBrush"];
            BtnNext.Foreground = Brushes.White;

            BtnNext.Content = "SHOW ME";
            Slider.Source = LoadBitmapFromResource("Resources/Slide1.png");
        }

        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            if(curSlide < slidesCount - 2)
                BtnNext.Content = "NEXT";
            else BtnNext.Content = "GOT IT";

            curSlide++;

            if (curSlide >= slidesCount)
            {
                Close();
                return;
            }

            var m = Slider.Margin;
            var anim = new ThicknessAnimation(m, new Thickness(-Slider.ActualWidth, 0,0,0), (Duration)TimeSpan.FromSeconds(0.3));
            anim.Completed += new EventHandler((o, ev) =>
                {
                    Slider.Source = LoadBitmapFromResource("Resources/Slide" + (curSlide + 1).ToString() + ".png");

                    var anim3 = new ThicknessAnimation(new Thickness(Slider.ActualWidth, 0, -Slider.ActualWidth, 0), new Thickness(0, 0, 0, 0), (Duration)TimeSpan.FromSeconds(0.3));
                    Slider.BeginAnimation(Window.MarginProperty, anim3, HandoffBehavior.SnapshotAndReplace);

                    var anim4 = new DoubleAnimation(0, 1, (Duration)TimeSpan.FromSeconds(0.3));
                    Slider.BeginAnimation(Window.OpacityProperty, anim4, HandoffBehavior.SnapshotAndReplace);

                });
            Slider.BeginAnimation(Window.MarginProperty, anim, HandoffBehavior.SnapshotAndReplace);

            var anim2 = new DoubleAnimation(1, 0, (Duration)TimeSpan.FromSeconds(0.3));
            Slider.BeginAnimation(Window.OpacityProperty, anim2, HandoffBehavior.SnapshotAndReplace);
        }
    }
}
