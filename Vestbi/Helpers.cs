using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Microsoft.Win32;
using Vestbi.Properties;
namespace Vestbi
{
    class Helpers
    {
        private static string regKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private static string regValue = "Vestbi";

        public static Size MeasureString(string candidate, UIElement uiElement)
        {
            dynamic element = null;

            if (uiElement is TextBox || uiElement is TextBlock)
                element = uiElement;

            try
            {
                var formattedText = new FormattedText(
                    candidate,
                    System.Globalization.CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    new Typeface(element.FontFamily, element.FontStyle, element.FontWeight, element.FontStretch),
                    element.FontSize,
                    Brushes.Black);

                return new Size(formattedText.Width, formattedText.Height);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                throw new ArgumentException("Only UIElements with font properties are supported");
            }
        }

        internal static Effect MakePulsingShadow(Color color, bool reverse = true, int repeat = 777)
        {
            DropShadowEffect dropShadowEffect = new DropShadowEffect();
            dropShadowEffect.ShadowDepth = 1;
            dropShadowEffect.Color = color;
            dropShadowEffect.Opacity = 0.7;
            dropShadowEffect.BlurRadius = 90;

            var anim = new DoubleAnimation(0, 50, (Duration)TimeSpan.FromSeconds(0.5));
            anim.RepeatBehavior = new RepeatBehavior(repeat);
            anim.EasingFunction = new PowerEase() { EasingMode = EasingMode.EaseOut };
            anim.AutoReverse = reverse;
            dropShadowEffect.BeginAnimation(DropShadowEffect.BlurRadiusProperty, anim, HandoffBehavior.SnapshotAndReplace);

            return dropShadowEffect;
        }

        public static void MakeStartup(bool startup)
        {
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey(regKey, true);

            if (startup && !IsStartupItem())
                rkApp.SetValue(regValue, "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"", RegistryValueKind.String);
            else if (!startup && IsStartupItem())
                rkApp.DeleteValue(regValue, false);
        }

        public static bool IsStartupItem()
        {
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey(regKey, true);
            if (rkApp.GetValue(regValue) == null)
                return false;
            else
                return true;
        }
    }

 

    public static class RestoreWindowNoActivateExtension
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, UInt32 nCmdShow);

        private const int SW_SHOWNOACTIVATE = 4;

        public static void RestoreNoActivate(this Window win)
        {
            WindowInteropHelper winHelper = new WindowInteropHelper(win);
            ShowWindow(winHelper.Handle, SW_SHOWNOACTIVATE);
        }
    }
}
