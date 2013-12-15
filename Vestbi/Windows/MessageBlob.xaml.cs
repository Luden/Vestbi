using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using System.Windows.Threading;
using MahApps.Metro;
using MahApps.Metro.Controls;

namespace Vestbi
{
    /// <summary>
    /// Simple tray popup blob
    /// </summary>
    public partial class MessageBlob : MetroWindow
    {
        public string Text
        {
            set { TbMessage.Text = value; }
        }

        public string Url
        {
            set 
            {
                if (value == "")
                {
                    TbMessage.TextDecorations = null;
                    TbMessage.Cursor = Cursors.Arrow;
                }
                else
                {
                    TbMessage.TextDecorations = TextDecorations.Underline;
                    TbMessage.Cursor = Cursors.Hand;
                    TbMessage.MouseDown += (o, e) =>
                        {
                            Process.Start(value);
                        };
                }
            }
        }

        DispatcherTimer timer = new DispatcherTimer();
        double fadeSeconds = 2;

        public MessageBlob()
        {
            InitializeComponent();
            Top = 3000;

            var currentAccent = ThemeManager.DefaultAccents.First(x => x.Name == ProgramSettings.Current.Accent);
            var currentTheme = ProgramSettings.Current.Theme == "Light" ? Theme.Light : Theme.Dark;
            ThemeManager.ChangeTheme(this, currentAccent, currentTheme);
        }

        private void MetroWindow_Loaded_1(object sender, RoutedEventArgs e)
        {
            timer.Interval = TimeSpan.FromSeconds(fadeSeconds);
            timer.Tick += TimerTick;
            timer.Start();

            ComputeHeight();

            var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
            var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));
            this.Left = corner.X - this.ActualWidth;
            this.Top = corner.Y + 50;

            FadeIn();
        }

        private void MetroWindow_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            BeginClosing();
        }

        void BeginClosing()
        {
            Closing -= MetroWindow_Closing_1;

            // random slide effect
            double animLength = 1;
            switch (new Random().Next(3))
            {
                case 0: // top to bottom
                    {
                        animLength = 1;
                        var anim = new DoubleAnimation(Top, Top + ActualHeight + 50, (Duration)TimeSpan.FromSeconds(animLength));
                        anim.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseInOut };
                        anim.Completed += (s, _) => this.Close();
                        this.BeginAnimation(Window.TopProperty, anim);
                        break;
                    }
                case 1: // left to right
                    {
                        animLength = 0.5;
                        var anim = new DoubleAnimation(Left, Left + Width, (Duration)TimeSpan.FromSeconds(animLength));
                        anim.EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut };
                        anim.Completed += (s, _) => this.Close();
                        this.BeginAnimation(Window.LeftProperty, anim);
                        break;
                    }
                case 2: // no slide
                    {
                        animLength = 0.5;
                        var timer = new DispatcherTimer() {
                            Interval = TimeSpan.FromSeconds(animLength),
                            IsEnabled = true                            
                        };
                        timer.Tick += new EventHandler((o, e) => { this.Close(); });
                        break;
                    }
            }

            // random fading effect
            var opAnim = new DoubleAnimation(1, 0, (Duration)TimeSpan.FromSeconds((new Random().NextDouble() + 0.5) * animLength));
            var ef = new EasingFunctionBase[] { new SineEase(), new QuinticEase(), new PowerEase(), new ExponentialEase() }.OrderBy(x => Guid.NewGuid()).First();
            ef.EasingMode = EasingMode.EaseIn;
            opAnim.EasingFunction = ef;
            this.BeginAnimation(Window.OpacityProperty, opAnim);
        }

        private void TimerTick(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            timer.Stop();
            timer.Tick -= TimerTick;
            BeginClosing();
        }

        public void FadeIn()
        {
            var w = Width;
            var h = Height;
            var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
            var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));

            var slideAnimTop = new DoubleAnimation(this.Top, corner.Y - h, (Duration)TimeSpan.FromSeconds(0.5));
            slideAnimTop.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut, Amplitude = 0.5};
            this.BeginAnimation(Window.TopProperty, slideAnimTop, HandoffBehavior.SnapshotAndReplace);
        }

        public void ComputeHeight()
        {
            var h = Helpers.MeasureString(TbMessage.Text, TbMessage).Height;
            var w = Helpers.MeasureString(TbMessage.Text, TbMessage).Width;
            Height = h + 20;
            Width = w + 20;
        }

        public static void ShowPopup(string message, double fadeTime = 2, string url = "")
        {
            var blob = new MessageBlob();
            blob.Text = message;
            blob.fadeSeconds = fadeTime;
            blob.Url = url;
            blob.Show();
        }

        private void MetroWindow_MouseEnter_1(object sender, MouseEventArgs e)
        {
            timer.Stop();
        }

        private void MetroWindow_MouseLeave_1(object sender, MouseEventArgs e)
        {
            timer.Start();
        }

        private void MetroWindow_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            BeginClosing();
        }
    }
}
