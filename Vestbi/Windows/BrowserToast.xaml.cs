using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MahApps.Metro;
using MahApps.Metro.Controls;

namespace Vestbi
{
    /// <summary>
    /// Interaction logic for BrowserToast.xaml
    /// </summary>
    public partial class BrowserToast : MetroWindow
    {
        DispatcherTimer timer = new DispatcherTimer();
        int fadeSeconds = 5;
        private string _url;
        public string Url 
        {
            set
            {
                _url = value;
                Title = value;
            }
            get
            {
                return _url;
            }
        }

        public BrowserToast()
        {
            InitializeComponent();
            Top = 1100;

            var currentAccent = ThemeManager.DefaultAccents.First(x => x.Name == ProgramSettings.Current.Accent);
            var currentTheme = ProgramSettings.Current.Theme == "Light" ? Theme.Light : Theme.Dark;
            ThemeManager.ChangeTheme(this, currentAccent, currentTheme);
        }

        private void WndToast_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            BeginClosing();
        }

        void BeginClosing()
        {
            Closing -= WndToast_Closing;
            var anim = new DoubleAnimation(Top, Top + ActualHeight, (Duration)TimeSpan.FromSeconds(0.3));
            anim.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseIn };
            anim.Completed += (s, _) => this.Close();
            this.BeginAnimation(Window.TopProperty, anim);
        }

        private void WndToast_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BeginClosing();
        }

        private void WndToast_Loaded(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false; // WebBrowser steal focus issue

            var url = Url; // "http://translate.google.ru/?hl=en&tab=wT&authuser=0#en/ru/text";

            HideScriptErrors(Browser, true);
            Browser.Navigate(url);
            Browser.LoadCompleted += Browser_LoadCompleted;
            Browser.ObjectForScripting = new ScriptingHelper(this);

            timer.Interval = TimeSpan.FromSeconds(fadeSeconds);
            timer.Tick += TimerTick;
            timer.Start();

            var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
            var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));
            this.Left = corner.X - this.ActualWidth;
            this.Top = corner.Y + 50;
        }

        private void TimerTick(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            timer.Stop();
            timer.Tick -= TimerTick;
            BeginClosing();
        }

        public void FadeIn(int w, int h)
        {
            var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
            var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));

            var slideAnimTop = new DoubleAnimation(this.Top, corner.Y - h, (Duration)TimeSpan.FromSeconds(0.5));
            slideAnimTop.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut };
            this.BeginAnimation(Window.TopProperty, slideAnimTop, HandoffBehavior.SnapshotAndReplace);

            this.Width = w;

            var slideAnimHeight = new DoubleAnimation(this.Height, h, (Duration)TimeSpan.FromSeconds(0.2));
            slideAnimHeight.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut };
            this.BeginAnimation(Window.HeightProperty, slideAnimHeight, HandoffBehavior.SnapshotAndReplace);
        }

        void Browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            WebBrowser wb = (WebBrowser)sender;

            string script3 =
            @"
                document.body.style.overflow ='hidden';
                document.getElementById('gt-ft-res').style.display = 'none';

                function Foo() {
                    var el = document.getElementById('gt-res-c');                    
                    el.scrollIntoView(true); 
                    var w1 = el.offsetWidth;
                    var h1 = el.offsetHeight;
                
                    var el2 = document.getElementById('gt-lc');

                    if(el2) {
                        h1 += el2.offsetHeight;
                        try {
                            var w2 = el2.getElementsByTagName('table')[0].parentNode.scrollWidth;
                            if(w2 > w1) {
                                w1 = w2;
                            }
                        }
                        catch(err) {
                        }
                    }
                    window.external.SetRealWidth(w1, h1);                    
                }

                setTimeout(Foo, 100);
                setTimeout(Foo, 200);
";

            wb.InvokeScript("execScript", new Object[] { script3, "JavaScript" });

            this.IsEnabled = true; // WebBrowser steal focus issue

            // set fade triggers
            mshtml.HTMLDocumentEvents2_Event doc = ((mshtml.HTMLDocumentEvents2_Event)Browser.Document);
            doc.onmousemove += doc_onmousemove;
        }

        void doc_onmousemove(mshtml.IHTMLEventObj pEvtObj)
        {
            timer.Stop();
            timer.Interval = TimeSpan.FromSeconds(fadeSeconds);
            timer.Start();
        }

        [ComVisible(true)]
        public class ScriptingHelper
        {
            Window _wnd;
            public ScriptingHelper(Window wnd)
            {
                _wnd = wnd;
            }

            public void SetRealWidth(string width, string height)
            {
                var w = int.Parse(width);
                var h = int.Parse(height);

                ((BrowserToast)_wnd).FadeIn(w, h + 150);
            }
        }

        public void HideScriptErrors(WebBrowser wb, bool hide)
        {
            var fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;
            var objComWebBrowser = fiComWebBrowser.GetValue(wb);
            if (objComWebBrowser == null)
            {
                wb.Loaded += (o, s) => HideScriptErrors(wb, hide); //In case we are to early
                return;
            }
            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { hide });
        }

        private void WndToast_MouseMove(object sender, MouseEventArgs e)
        {
            timer.Interval = TimeSpan.FromSeconds(fadeSeconds);
        }

        private void WndToast_MouseEnter(object sender, MouseEventArgs e)
        {
            timer.Stop();
            timer.Interval = TimeSpan.FromSeconds(fadeSeconds);
            timer.Start();
        }

        private void WndToast_MouseLeave(object sender, MouseEventArgs e)
        {
            timer.Stop();
            timer.Interval = TimeSpan.FromSeconds(fadeSeconds);
            timer.Start();
        }

        public static void ShowToast(string url)
        {
            var toast = new BrowserToast();
            toast.Url = url;
            toast.Show();
        }
    }
}
