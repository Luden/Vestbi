using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using MahApps.Metro;
using MahApps.Metro.Controls;
using Ownskit.Utils;

namespace Vestbi
{
    public partial class MainWindow : MetroWindow
    {

        #region Properties helpers

        abstract class PropertyBinder
        {
            public abstract Type Type { get; }
            public string Name;
        }

        class PropertyBinder<T> : PropertyBinder
        {
            public Action<T> Setter;
            public Func<T> Getter;
            public PropertyBinder(string name, Action<T> setter, Func<T> getter)
            {
                Name = name;
                Setter = setter;
                Getter = getter;
            }
            public override Type Type
            {
                get { return typeof(T); }
            }
        }

        #endregion

        #region Appearence

        private Theme currentTheme = Theme.Light;
        private Accent currentAccent = ThemeManager.DefaultAccents.First(x => x.Name == "Blue");
        System.Windows.Forms.NotifyIcon _trayIcon;

        #endregion

        #region Animation toggles and stuff

        bool _allowSizeAnim = false;
        bool _optionsShown = false;
        bool _programOptionsShown = false;
        Thickness originalMainButtonPos = new Thickness();

        #endregion

        #region Managers

        Binder _binder = new Binder();
        KeyboardHook _hook = new KeyboardHook();

        #endregion

        #region CTOR
        public MainWindow()
        {
            InitializeComponent();

            //Debugger.Break();

            AddTrayIcon();
            LoadSettings();
            CheckForUpdates();

            Helpers.MakeStartup(ProgramSettings.Current.Autostart);

            ThemeManager.ChangeTheme(this, this.currentAccent, this.currentTheme);

            originalMainButtonPos = BtnOk.Margin;
            _hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);

            // some metro ui hardcoded customizations
            Resources["GrayBrush8"] = Resources["WindowTitleColorBrush"];
            Resources["GrayBrush5"] = Resources["HighlightBrush"];
            Resources["BlackBrush"] = Resources["WindowTitleColorBrush"];

            BtnOk.Foreground = Brushes.White;
            BtnBuild.Foreground = Brushes.White;

            if (ProgramSettings.Current.Minimized)
            {
                MakeBindings();
                Hide();
                return;
            }

            if (!ProgramSettings.Current.GuideShown)
            {
                ProgramSettings.Current.GuideShown = true;
                var guide = new Guide();
                guide.ShowDialog();
            }

            //int index = 0;
            //foreach (var br in Resources.MergedDictionaries[0].Values)
            //{
            //    if (br is SolidColorBrush)
            //    {
            //        //if (index > 50)
            //        {
            //            ((SolidColorBrush)br).Color = Colors.Red;
            //        }
            //    }
            //    index++;
            //}
        }
        #endregion
        
        #region Methods

        internal void CheckForUpdates()
        {
            var verUrl = "http://luden.github.io/Vestbi/Version";
            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler((o, e) =>
            {
                if (e.Error == null && !e.Cancelled)
                {
                    var res = e.Result;
                    var match = Regex.Match(res, "(\\d+).(\\d+).(\\d+).(\\d+)");
                    if (match.Success)
                    {
                        int majVer = int.Parse(match.Groups[1].Value);
                        int minVer = int.Parse(match.Groups[2].Value);

                        var ver = Assembly.GetExecutingAssembly().GetName().Version;
                        if (majVer > ver.Major || (majVer == ver.Major && minVer > ver.Minor))
                        {
                            BtnUpdate.Visibility = Visibility.Visible;
                        }
                    }
                }
            });
            WebRequest.DefaultWebProxy = null; // stupid lag
            client.DownloadStringAsync(new Uri(verUrl));
        }

        string GetKeyString(ModifierKeys mods, System.Windows.Forms.Keys key)
        {
            var res = new List<string>();

            if ((mods & ModifierKeys.Control) != 0)
                res.Add("CTRL");
            if ((mods & ModifierKeys.Shift) != 0)
                res.Add("SHIFT");
            if ((mods & ModifierKeys.Alt) != 0)
                res.Add("ALT");
            if ((mods & ModifierKeys.Win) != 0)
                res.Add("WIN");

            var wpfKey = KeyInterop.KeyFromVirtualKey((int)key);
            res.Add(wpfKey.ToString());

            return string.Join(" + ", res);
        }

        bool GetModifierAndkey(string str, out ModifierKeys modifiers, out System.Windows.Forms.Keys key)
        {
            modifiers = ModifierKeys.None;

            var list = new List<Key>();
            key = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(_binder.GetKeylistFromText(str, list));


            if (list.Contains(Key.LeftCtrl))
                modifiers |= ModifierKeys.Control;
            if (list.Contains(Key.LeftShift))
                modifiers |= ModifierKeys.Shift;
            if (list.Contains(Key.LeftAlt))
                modifiers |= ModifierKeys.Alt;
            if (list.Contains(Key.LWin))
                modifiers |= ModifierKeys.Win;

            return key != System.Windows.Forms.Keys.None;
        }

        private bool MakeBindings()
        {
            var key = System.Windows.Forms.Keys.None;
            var mods = ModifierKeys.None;

            _hook.UnregisterAllKeys();

            var keys = new List<string>();
            keys.AddRange(new string[] { ProgramSettings.Current.kRun, ProgramSettings.Current.kBrowse, 
                ProgramSettings.Current.kTranslate, ProgramSettings.Current.kRegex, ProgramSettings.Current.kScript,  
                ProgramSettings.Current.kAppend});

            var errMess = new List<string>();
            foreach (var str in keys.Distinct())
            {
                try
                {
                    if (GetModifierAndkey(str, out mods, out key))
                        _hook.RegisterHotKey(mods, key);
                }
                catch (System.Exception ex)
                {
                    errMess.Add(string.Format("{0} {1}", ex.Message, str));
                }
            }
            if (errMess.Any())
            {
                MessageBlob.ShowPopup(string.Join(Environment.NewLine, errMess));
                return false;
            }

            return true;
        }

        public void LoadSettings()
        {
            ProgramSettings.Load();
            TbCmd.Text = ProgramSettings.Current.cmd;
            TbUrl.Text = ProgramSettings.Current.url;
            TbBrowse.Text = ProgramSettings.Current.kBrowse;
            TbRegex.Text = ProgramSettings.Current.kRegex;
            TbRun.Text = ProgramSettings.Current.kRun;
            TbTranslate.Text = ProgramSettings.Current.kTranslate;
            TbRegexFrom.Text = ProgramSettings.Current.regexFrom;
            TbRegexTo.Text = ProgramSettings.Current.regexTo;
            TbFile.Text = ProgramSettings.Current.appendFile;
            TbAppend.Text = ProgramSettings.Current.kAppend;
            TbTranslateFrom.Text = ProgramSettings.Current.lngTranslateFrom;
            TbTranslateTo.Text = ProgramSettings.Current.lngTranslateTo;
            TbScript.Text = ProgramSettings.Current.kScript;
            CbAutostart.IsChecked = ProgramSettings.Current.Autostart;
            CbMinimized.IsChecked = ProgramSettings.Current.Minimized;

            currentTheme = ProgramSettings.Current.Theme == "Light" ? Theme.Light : Theme.Dark;
            currentAccent = ThemeManager.DefaultAccents.Any(x => x.Name == ProgramSettings.Current.Accent) ?
                ThemeManager.DefaultAccents.First(x => x.Name == ProgramSettings.Current.Accent)
                : ThemeManager.DefaultAccents.First(x => x.Name == "Blue");

            RbThemeDark.IsChecked = currentTheme == Theme.Dark;
            RbThemeLight.IsChecked = !RbThemeDark.IsChecked;

            ScriptManager.InitCodeEditor(CodeEditor);
            ScriptManager.SetHighlight(CodeEditor, ProgramSettings.Current.Theme == "Dark");
        }

        public void SaveSettings()
        {
            ProgramSettings.Current.cmd = TbCmd.Text;
            ProgramSettings.Current.url = TbUrl.Text;
            ProgramSettings.Current.kBrowse = TbBrowse.Text;
            ProgramSettings.Current.kRegex = TbRegex.Text;
            ProgramSettings.Current.kRun = TbRun.Text;
            ProgramSettings.Current.regexFrom = TbRegexFrom.Text;
            ProgramSettings.Current.regexTo = TbRegexTo.Text;
            ProgramSettings.Current.appendFile = TbFile.Text;
            ProgramSettings.Current.kAppend = TbAppend.Text;
            ProgramSettings.Current.kTranslate = TbTranslate.Text;
            ProgramSettings.Current.lngTranslateFrom = TbTranslateFrom.Text;
            ProgramSettings.Current.lngTranslateTo = TbTranslateTo.Text;
            ProgramSettings.Current.kScript = TbScript.Text;
            ProgramSettings.Current.Theme = currentTheme == Theme.Light ? "Light" : "Dark";
            ProgramSettings.Current.Accent = currentAccent.Name;
            ProgramSettings.Current.Autostart = CbAutostart.IsChecked ?? false;
            ProgramSettings.Current.Minimized = CbMinimized.IsChecked ?? false;

            CodeEditor.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProgramSettings.Current.scriptFile));

            ProgramSettings.Save();
        }

        void ShowActivate()
        {
            _hook.UnregisterAllKeys();
            this.Show();
            this.Activate();
            this.WindowState = WindowState.Normal;
        }

        void AddTrayIcon()
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon();
            _trayIcon.Icon = Properties.Resources.iconSmall;
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += (o, e) => ShowActivate();
            _trayIcon.MouseClick += (o, e) =>
                {
                    if (e.Button == System.Windows.Forms.MouseButtons.Right)
                    {
                        var menu = new ContextMenu();

                        var openButton = new MenuItem() { };
                        openButton.Header = "Open";
                        openButton.Click += (oi, ei) => ShowActivate();
                        menu.Items.Add(openButton);

                        var closeButton = new MenuItem();
                        closeButton.Header = "Exit";
                        closeButton.Click += (oi, ei) => Close();
                        menu.Items.Add(closeButton);

                        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
                        menu.IsOpen = true;
                    }
                };
        }

        ContextMenu CreateContextMenu(Button placement, params PropertyBinder[] options)
        {
            // all alignments - pure magic

            var size = Helpers.MeasureString(options.OrderByDescending(s => s.Name.Length).First().Name, new TextBlock());
            var height = options.Count() * (size.Height + 10) + 16;
            var width = size.Width + 66;

            var menu = new ContextMenu();

            menu.BorderThickness = new Thickness(1);
            menu.BorderBrush = System.Windows.Media.Brushes.White;
            menu.Closed += ContextMenu_Closed_1;
            menu.PlacementTarget = placement;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
            var location = placement.PointToScreen(new Point(0, 0));
            

            foreach (var option in options)
            {
                if (option.Type == typeof(bool))
                {
                    var curOption = option as PropertyBinder<bool>;
                    var textblock = new TextBlock();
                    textblock.Text = curOption.Name;

                    var checkbox = new CheckBox();
                    checkbox.IsChecked = curOption.Getter();
                    var handler = new RoutedEventHandler((o, e) =>
                    {
                        curOption.Setter(checkbox.IsChecked ?? false);
                    });
                    checkbox.Checked += handler;
                    checkbox.Unchecked += handler;
                    checkbox.Margin = new Thickness(6, 0, 0, 0);

                    var stack = new StackPanel();
                    stack.HorizontalAlignment = HorizontalAlignment.Right;
                    stack.Orientation = Orientation.Horizontal;
                    stack.Children.Add(textblock);
                    stack.Children.Add(checkbox);
                    stack.Margin = new Thickness(0, 0, -15, 0);

                    var item = new MenuItem();
                    item.StaysOpenOnClick = true;
                    item.Header = stack;
                    item.Click += new RoutedEventHandler((o, e) =>
                    {
                        checkbox.IsChecked = !checkbox.IsChecked;
                    });

                    menu.Items.Add(item);
                }
                else if (option.Type == typeof(Encoding))
                {
                    var curOption = option as PropertyBinder<Encoding>;
                    
                    var textblock = new TextBlock();
                    textblock.Text = curOption.Name;
                    textblock.Margin = new Thickness(0, -2, 0, 0);
                    textblock.TextAlignment = TextAlignment.Right;

                    var combobox = new ComboBox();
                    foreach (var encoding in Encoding.GetEncodings())
                    {
                        var item = new ComboBoxItem();
                        item.Content = encoding.Name;
                        item.HorizontalContentAlignment = HorizontalAlignment.Right;
                        combobox.Items.Add(item);
                        if (encoding.Name == curOption.Getter().HeaderName)
                            combobox.SelectedIndex = combobox.Items.Count - 1;
                    }
                    combobox.SelectionChanged += (o, e) =>
                        {
                            curOption.Setter(Encoding.GetEncoding((combobox.SelectedItem as ComboBoxItem).Content.ToString()));
                        };
                    combobox.HorizontalContentAlignment = HorizontalAlignment.Right;
                    combobox.Width = 120;
                    combobox.Margin = new Thickness(0, 6, 0, 0);

                    var stack = new StackPanel();
                    stack.HorizontalAlignment = HorizontalAlignment.Right;
                    stack.Children.Add(textblock);
                    stack.Children.Add(combobox);
                    stack.Margin = new Thickness(0, 0, -9, 0);

                    var menuItem = new MenuItem();
                    menuItem.StaysOpenOnClick = true;
                    menuItem.Header = stack;
                    menu.Items.Add(menuItem);
                    height += 20;
                }
            }

            menu.PlacementRectangle = new System.Windows.Rect(location.X + placement.Width - width + 5, location.Y + placement.Height - 5,
                width, height);

            menu.IsOpen = true;
            menu.Width = width;
            menu.Height = height;
            var anim = new DoubleAnimation(0, height, (Duration)TimeSpan.FromSeconds(0.2));
            menu.BeginAnimation(Window.HeightProperty, anim, HandoffBehavior.SnapshotAndReplace);

            var anim2 = new DoubleAnimation(0.5, 1, (Duration)TimeSpan.FromSeconds(0.2));
            menu.BeginAnimation(Window.OpacityProperty, anim2, HandoffBehavior.SnapshotAndReplace);

            return menu;
        }

        private void ChangeTheme(object sender, RoutedEventArgs e)
        {
            currentTheme = RbThemeDark.IsChecked == true ? Theme.Dark : Theme.Light;
            ThemeManager.ChangeTheme(this, currentAccent, this.currentTheme);
            ProgramSettings.Current.Theme = RbThemeDark.IsChecked == true ? "Dark" : "Light";
            if (CodeEditor != null)
                ScriptManager.SetHighlight(CodeEditor, RbThemeDark.IsChecked == true);
        }

        void BuildScript()
        {
            CodeEditor.SaveFile();
            MessageBlob.ShowPopup(ScriptManager.Compile());
        }

        void ShowOptions(bool show = true)
        {
            if (_programOptionsShown == show)
                return;

            _programOptionsShown = show;
            var m = GridOptions.Margin;
            var dm = new Thickness(show ? -10 : -GridOptions.ActualWidth, m.Top, m.Right, m.Bottom);
            var anim = new ThicknessAnimation(m, dm, (Duration)TimeSpan.FromSeconds(0.5));
            anim.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
            GridOptions.BeginAnimation(Grid.MarginProperty, anim, HandoffBehavior.SnapshotAndReplace);
        }

        #endregion

        #region Event handlers

        private void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            var keyStr = GetKeyString(e.Modifier, e.Key);

            if (keyStr == ProgramSettings.Current.kRun)
                CoreMethods.Run(ProgramSettings.Current.cmd);

            if (keyStr == ProgramSettings.Current.kBrowse)
                CoreMethods.Browse(ProgramSettings.Current.url);

            if (keyStr == ProgramSettings.Current.kTranslate)
                CoreMethods.Translate();

            if (keyStr == ProgramSettings.Current.kRegex)
                CoreMethods.Regex(ProgramSettings.Current.regexFrom, ProgramSettings.Current.regexTo);

            if (keyStr == ProgramSettings.Current.kAppend)
                CoreMethods.AppendToFile(ProgramSettings.Current.appendFile);

            if (keyStr == ProgramSettings.Current.kScript)
                CoreMethods.ExecuteScript();
        }
        
        private void TextBox_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            ShowOptions(false);
            var tb = sender as TextBox;
            _binder.Stop();
            _binder.Start(tb);
            TbHidden.Focus();
        }

        private void MetroWindow_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _binder.Dispose();
            _trayIcon.Visible = false;
            SaveSettings();
        }

        private void MetroWindow_StateChanged_1(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
                _binder.Stop();
                SaveSettings();
                MakeBindings();
                Hide();
                return;
            }
        }

        private void TbHidden_LostFocus(object sender, RoutedEventArgs e)
        {
            _binder.Stop();
        }

        private void TbRun_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TbHidden_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && _binder.IsWorking())
            {
                _binder.Stop();
                e.Handled = true;
            }
        }

        private void ContextMenu_Closed_1(object sender, RoutedEventArgs e)
        {
            _optionsShown = false;
            FadeRect.Visibility = Visibility.Hidden;
        }

        private void RunOptionsClick(object sender, RoutedEventArgs e)
        {
            if (!_optionsShown)
            {
                _optionsShown = true;
                FadeRect.Visibility = Visibility.Visible;
                this.ContextMenu = CreateContextMenu(sender as Button,
                    new PropertyBinder<bool>("Ask if no text selected", o => ProgramSettings.Current.cmdAsk = o, () => ProgramSettings.Current.cmdAsk),
                    new PropertyBinder<bool>("Hide window", o => ProgramSettings.Current.cmdHide = o, () => ProgramSettings.Current.cmdHide),
                    new PropertyBinder<bool>("Wait completion", o => ProgramSettings.Current.cmdWait = o, () => ProgramSettings.Current.cmdWait),
                    new PropertyBinder<bool>("Copy to clipboard", o => ProgramSettings.Current.cmdCopyToClipboard = o, () => ProgramSettings.Current.cmdCopyToClipboard),
                    new PropertyBinder<bool>("Paste results", o => ProgramSettings.Current.cmdPasteToOutput = o, () => ProgramSettings.Current.cmdPasteToOutput),
                    new PropertyBinder<bool>("Show results", o => ProgramSettings.Current.cmdPopResults = o, () => ProgramSettings.Current.cmdPopResults),
                    new PropertyBinder<Encoding>("Results encoding", o => ProgramSettings.Current.cmdEncoding = o.HeaderName,
                        () => 
                            {
                                try 
                                { 
                                    return Encoding.GetEncoding(ProgramSettings.Current.cmdEncoding);
                                } 
                                catch (Exception ex) 
                                { return Encoding.Default;}
                            }
                    ));
            }
        }

        private void BrowseOptionsClick(object sender, RoutedEventArgs e)
        {
            if (!_optionsShown)
            {
                _optionsShown = true;
                FadeRect.Visibility = Visibility.Visible;
                this.ContextMenu = CreateContextMenu(sender as Button,
                    new PropertyBinder<bool>("Encode url", o => ProgramSettings.Current.bEncodeUrl = o, () => ProgramSettings.Current.bEncodeUrl)
                    );
            }
        }

        private void RegexOptionsClick(object sender, RoutedEventArgs e)
        {
            if (!_optionsShown)
            {
                _optionsShown = true;
                FadeRect.Visibility = Visibility.Visible;
                this.ContextMenu = CreateContextMenu(sender as Button,
                    new PropertyBinder<bool>("Copy to clipboard", o => ProgramSettings.Current.bRegexCopyToClipboard = o, () => ProgramSettings.Current.bRegexCopyToClipboard),
                    new PropertyBinder<bool>("Paste results", o => ProgramSettings.Current.bRegexPasteToOutput = o, () => ProgramSettings.Current.bRegexPasteToOutput),
                    new PropertyBinder<bool>("Pop results", o => ProgramSettings.Current.bRegexPopResults = o, () => ProgramSettings.Current.bRegexPopResults)
                    );
            }
        }

        private void ScriptOptionsClick(object sender, RoutedEventArgs e)
        {
            if (!_optionsShown)
            {
                _optionsShown = true;
                FadeRect.Visibility = Visibility.Visible;
                this.ContextMenu = CreateContextMenu(sender as Button,
                    new PropertyBinder<bool>("Ask if no text selected", o => ProgramSettings.Current.bScriptAsk = o, () => ProgramSettings.Current.bScriptAsk),
                    new PropertyBinder<bool>("Copy to clipboard", o => ProgramSettings.Current.bScriptCopyToClipboard = o, () => ProgramSettings.Current.bScriptCopyToClipboard),
                    new PropertyBinder<bool>("Paste results", o => ProgramSettings.Current.bScriptPasteToOutput = o, () => ProgramSettings.Current.bScriptPasteToOutput),
                    new PropertyBinder<bool>("Pop results", o => ProgramSettings.Current.bScriptPopResults = o, () => ProgramSettings.Current.bScriptPopResults)
                    );
            }
        }

        private void MetroWindow_ContextMenuOpening_1(object sender, ContextMenuEventArgs e)
        {
            if (!_optionsShown)
                e.Handled = true;
        }

        private void MetroWindow_Loaded_1(object sender, RoutedEventArgs e)
        {
            _allowSizeAnim = true;

            this.Opacity = 0;
            var opAnim = new DoubleAnimation(0, 1, (Duration)TimeSpan.FromSeconds(0.5));
            this.BeginAnimation(Window.OpacityProperty, opAnim);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            _binder.Stop();

            ShowOptions(false);
            SaveSettings();
            if (MakeBindings())
                Hide();
        }

        private void Button_MouseEnter_1(object sender, MouseEventArgs e)
        {
            UIElement element = (UIElement)sender;

            if (element is TextBox)
            {
                var tb = element as TextBox;
            }

            DropShadowEffect dropShadowEffect = new DropShadowEffect();
            dropShadowEffect.ShadowDepth = 0;
            dropShadowEffect.Color = (Color)Resources["AccentColor"];
            
            dropShadowEffect.Opacity = currentTheme == Theme.Dark ? 1 : 0.5;
            dropShadowEffect.BlurRadius = 9;

            element.Effect = dropShadowEffect;
        }

        private void MainButton_MouseEnter(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;

            var anim2 = new ThicknessAnimation(button.Margin, new Thickness(0, 0, originalMainButtonPos.Right + 20, originalMainButtonPos.Bottom), 
                (Duration)TimeSpan.FromSeconds(0.5));
            anim2.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut };
            button.BeginAnimation(Button.MarginProperty, anim2, HandoffBehavior.SnapshotAndReplace);

            button.Effect = Helpers.MakePulsingShadow((Color)Resources["AccentColor"]);
        }

        private void MainButton_MouseLeave(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;
            button.Effect = null;

            var anim2 = new ThicknessAnimation(button.Margin, originalMainButtonPos,
                (Duration)TimeSpan.FromSeconds(0.2));
            anim2.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut };
            button.BeginAnimation(Button.MarginProperty, anim2, HandoffBehavior.SnapshotAndReplace);
        }

        private void Button_MouseLeave_1(object sender, MouseEventArgs e)
        {
            UIElement myTextBox = (UIElement)sender;
            myTextBox.Effect = null;
        }

        private void MetroWindow_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            Tabs.TabStripMargin = new Thickness(0, ActualHeight - (362 - 288), 0, 0);
        }
      
        private void Tabs_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (!_allowSizeAnim)
                return;

            ShowOptions(false);

            double newMinHeight = MinHeight;
            if (TabAdvanced.IsSelected)
                newMinHeight = ActualHeight > MinHeight ? ActualHeight : MinHeight;
            else if (TabProfessional.IsSelected)
                newMinHeight = ActualHeight > 455 ? ActualHeight : 455;

            if (Math.Abs(newMinHeight - ActualHeight) < 10)
                return;

            // mahapps metro dont like top animation (glow border breaks). so use timer instead
            var newTop = Top - (newMinHeight - ActualHeight) / 2;
            var step = (newTop - Top) / 10;
            var tm = new DispatcherTimer();
            tm.Interval = TimeSpan.FromMilliseconds(10);
            tm.Tick += new EventHandler((o, ev) =>
                {
                    Top += step;
                    if (Math.Abs(newTop - Top) < Math.Abs(step * 2))
                    {
                        (o as DispatcherTimer).Stop();
                        var animHeight = new DoubleAnimation(ActualHeight, newMinHeight, (Duration)TimeSpan.FromSeconds(Math.Abs(newMinHeight - ActualHeight) / 200));
                        animHeight.EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
                        this.BeginAnimation(Window.HeightProperty, animHeight);
                    }
                });
            tm.Start();
        }

        private void AccentSelectorClick(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            btn.ContextMenu.IsOpen = true;
        }

        private void TextBlock_MouseEnter_1(object sender, MouseEventArgs e)
        {
            TextBlock text = sender as TextBlock;
            var anim = new DoubleAnimation(text.Opacity, 0.15, (Duration)TimeSpan.FromSeconds(0.5));
            anim.RepeatBehavior = new RepeatBehavior(777);
            anim.EasingFunction = new PowerEase() { EasingMode = EasingMode.EaseOut };
            anim.AutoReverse = true;
            text.BeginAnimation(TextBlock.OpacityProperty, anim, HandoffBehavior.SnapshotAndReplace);
        }

        private void TextBlock_MouseLeave_1(object sender, MouseEventArgs e)
        {
            TextBlock text = sender as TextBlock;
            var anim = new DoubleAnimation(text.Opacity, 0.01, (Duration)TimeSpan.FromSeconds(0.5));
            anim.EasingFunction = new PowerEase() { EasingMode = EasingMode.EaseOut };
            text.BeginAnimation(TextBlock.OpacityProperty, anim, HandoffBehavior.SnapshotAndReplace);
        }

        private void ChangeAccent(object sender, RoutedEventArgs e)
        {
            ChangeAccent((string)((MenuItem)sender).Header);
        }

        private void ChangeAccent(string accentName)
        {
            this.currentAccent = ThemeManager.DefaultAccents.First(x => x.Name == accentName);
            ThemeManager.ChangeTheme(this, this.currentAccent, this.currentTheme);
            ProgramSettings.Current.Accent = accentName;
        }

        private void AccentMenuMouseLeave(object sender, MouseEventArgs e)
        {
            ThemeManager.ChangeTheme(this, this.currentAccent, this.currentTheme);
        }

        private void AccentButtonMouseEnter(object sender, MouseEventArgs e)
        {
            var accentName = (string)((MenuItem)sender).Header;
            var currentAccent = ThemeManager.DefaultAccents.First(x => x.Name == accentName);
            ThemeManager.ChangeTheme(this, currentAccent, this.currentTheme);
        }

        private void ClickThemeMenuItem(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem.Header == RbThemeDark)
            {
                RbThemeDark.IsChecked = !RbThemeDark.IsChecked;
                RbThemeLight.IsChecked = !RbThemeDark.IsChecked;
            }
            else
            {
                RbThemeLight.IsChecked = !RbThemeLight.IsChecked;
                RbThemeDark.IsChecked = !RbThemeLight.IsChecked;
            }
        }

        private void ButtonBuildClick(object sender, RoutedEventArgs e)
        {
            BuildScript();
        }

        private void CodeEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
                BuildScript();
        }

        private void CopyleftMousedown(object sender, MouseButtonEventArgs e)
        {
            var about = new About() { Owner = this };
            about.ShowDialog();
        }

        private void UpdateButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start("http://sourceforge.net/projects/vestbi/files/latest/download");
        }

        private void BtnOptionsClick(object sender, RoutedEventArgs e)
        {
            ShowOptions(!_programOptionsShown);
        }

        #endregion
    }
}
