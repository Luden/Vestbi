using System;
using System.Collections.Generic;
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
using MahApps.Metro;
using MahApps.Metro.Controls;

namespace Vestbi
{
    /// <summary>
    /// Interaction logic for Popup.xaml
    /// </summary>
    public partial class Popup : MetroWindow
    {
        string _text = "";
        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        public Popup(string text = "")
        {
            InitializeComponent();
            TbText.Focus();
            if (text != "")
            {
                TbText.Text = text;
                TbText.SelectAll();
            }

            var currentAccent = ThemeManager.DefaultAccents.First(x => x.Name == ProgramSettings.Current.Accent);
            var currentTheme = ProgramSettings.Current.Theme == "Light" ? Theme.Light : Theme.Dark;
            ThemeManager.ChangeTheme(this, currentAccent, currentTheme);

            DataObject.AddPastingHandler(TbText, new DataObjectPastingEventHandler(OnPaste));
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var isText = e.SourceDataObject.GetDataPresent(System.Windows.DataFormats.Text, true);
            if (!isText) return;

            var text = e.SourceDataObject.GetData(DataFormats.Text) as string;

            var ci = TbText.CaretIndex;
            TbText.Text = TbText.Text.Insert(ci, text);
            TbText.CaretIndex = ci + text.Length;

            e.Handled = true;
        }

        private void TextBox_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                var ci = TbText.CaretIndex;
                TbText.Text = TbText.Text.Insert(ci, Environment.NewLine);
                TbText.CaretIndex = ci + Environment.NewLine.Length;
            }
            else if (e.Key == Key.Enter)
            {
                _text = TbText.Text;
                DialogResult = true;
            }
            else if (e.Key == Key.Escape)
            {
                _text = TbText.Text;
                DialogResult = false;
            }
        }

        public static bool ShowDialog(ref string str)
        {
            var popup = new Popup(str);
            var res = popup.ShowDialog();
            str = popup.Text;
            return res??false;
        }

        private void PopupWnd_Deactivated(object sender, EventArgs e)
        {
            if (DialogResult == null) // win8 compatibility
            {
                DialogResult = false;
                Close();
            }
        }

        private void PopupWnd_ContentRendered(object sender, EventArgs e)
        {
            try
            {
                //this.GlobalActivate();
                Activate();
            }
            catch (System.Exception ex)
            {
            }
        }

        private void Resize(int width, int height)
        {
            GlowBrush = Brushes.Transparent;
            TbText.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            var animHeight = new DoubleAnimation(ActualHeight, height, (Duration)TimeSpan.FromSeconds(1));
            animHeight.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
            this.BeginAnimation(Window.HeightProperty, animHeight);

            var animTop = new DoubleAnimation(Top, Top - (height - ActualHeight) / 2, (Duration)TimeSpan.FromSeconds(1));
            animTop.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
            this.BeginAnimation(Window.TopProperty, animTop);

            
            var animWidth = new DoubleAnimation(ActualWidth, width, (Duration)TimeSpan.FromSeconds(1));
            animWidth.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
            this.BeginAnimation(Window.WidthProperty, animWidth);

            var animLeft = new DoubleAnimation(Left, Left - (width - ActualWidth) / 2, (Duration)TimeSpan.FromSeconds(1));
            animLeft.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
            this.BeginAnimation(Window.LeftProperty, animLeft);
        }

        private void TbText_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            var h = Helpers.MeasureString(TbText.Text, TbText).Height;
            var w = Helpers.MeasureString(TbText.Text, TbText).Width;

            if (w + 10 > TbText.ActualWidth || h + 10 > TbText.ActualHeight)
            {
                Resize(800, 600);
                TbWatermark.Visibility = Visibility.Visible;
            }
        }
    }
}
