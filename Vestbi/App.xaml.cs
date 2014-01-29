using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace Vestbi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [System.Security.SuppressUnmanagedCodeSecurity]
        internal class NativeMethods
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("user32")]
            public static extern int RegisterWindowMessage(string message);

            public static readonly int WM_SHOWACTIVATE_VESTBI = RegisterWindowMessage("WM_SHOWACTIVATE_VESTBI");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // single instance realization through FindWindow

            IntPtr hTargetWnd = NativeMethods.FindWindow(null, Vestbi.MainWindow.VestbiTitle);
            if (hTargetWnd != IntPtr.Zero)
            {
                var res = NativeMethods.SendMessage(hTargetWnd, NativeMethods.WM_SHOWACTIVATE_VESTBI, new IntPtr(), new IntPtr());

                // If ther is more than one, than it is already running.
                System.Windows.Application.Current.Shutdown(0);
                return;
            }

            this.StartupUri = new Uri("Windows/MainWindow.xaml", UriKind.Relative); // do not ctor window if Shutdown initiated

            AppDomain.CurrentDomain.FirstChanceException += FirstChanceHandler;

            base.OnStartup(e);
        }

        static void FirstChanceHandler(object source, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is System.InvalidOperationException)
            {
                int zzz = 1;
            }
        }
    }
}
