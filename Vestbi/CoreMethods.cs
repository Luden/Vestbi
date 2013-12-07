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

namespace Vestbi
{
    class CoreMethods
    {
        static string arg_pattern = "{text}";

        /// <summary>
        /// 
        /// Distilled quintessence of pure unadulterated shitcode
        /// 
        /// Attempt to get selected text from any app, sending ctrl+c to it and retreiving clipboard data
        /// but... yes, here comes 'but'.
        /// 1. No one should use clipboard such stupid way!
        /// 2. I am trying to backup previous clipboard data. It can be unexplainably accidentally lost, of course.
        /// 3. Multithreaded apps. Shitballs
        /// 3.1 Event-based approach AddClipboardFormatListener, WM_CLIPBOARDUPDATE - not working
        /// 3.2 Yielding thread many times - not working
        /// 3.3 WinApi bool OpenClipboard - not working
        /// 3.4 System.Windows.Forms.Clipboard and System.Windows.Clipboard - no difference
        /// so i'm just trying many times, waitin in hope and gotta-catch-em-all'ing exceptions. 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static bool GetSelectedText(ref string str)
        {
            var data = new Dictionary<string, object>();
            str = "";
            var bkfStr = "";

            try
            {
                // try backup clipboard
                try
                {
                    var dob = Clipboard.GetDataObject();
                    foreach (var format in dob.GetFormats())
                        data[format] = dob.GetData(format);
                    bkfStr = Clipboard.GetText();
                }
                catch (System.Exception ex)
                { }

                // try copy selected text

                Clipboard.Clear();


                System.Threading.Thread.Sleep(100);

                System.Windows.Forms.SendKeys.SendWait("^c"); // ctrl+c in your face! it will destroy your console app, but I still dont care
                System.Windows.Forms.SendKeys.SendWait("^c"); // whos dat dumbass who needs to be told twice?! opera! (actually its multithreading issue, again)
                System.Threading.Thread.Sleep(100); // naive attemption to wait for app to response ctrl+c

                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        System.Threading.Thread.Yield(); // stupid multithreading apps breaking my creation! mu ha ha! hm... damn chrome!

                        str = "";
                        var fmts = Clipboard.GetDataObject().GetFormats();
                        if (!fmts.Contains("VisualStudioEditorOperationsLineCutCopyClipboardTag")) // visual studio likes to copy line of code if nothing selected
                            str = Clipboard.GetText();
                    }
                    catch (System.Exception ex) // here goes dat slowest part. wait eternity, please
                    {
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }

                    break; // success! yippee ki-yay!
                }

                Clipboard.Clear();

                // try restore clipboard
                try
                {
                    var newDob = new DataObject();
                    foreach (var format in data)
                        newDob.SetData(format.Key, format.Value);
                    Clipboard.SetDataObject(newDob);
                }
                catch (System.Exception ex) // i dont give any shit about your uber-important data in clipboard!
                { }
            }
            catch (System.Exception ex) // Clipboard.Clear trow exception sometimes. sometimes dont. but sometimes do
            { }

            if(str == "")
            {
                //str = bkfStr;
                return false;
            }
            return true;
        }

        static void SetSelectedText(string str)
        {
            var data = new Dictionary<string, object>();

            try
            {
                // try backup clipboard
                try
                {
                    var dob = Clipboard.GetDataObject();
                    foreach (var format in dob.GetFormats())
                        data[format] = dob.GetData(format);
                }
                catch (System.Exception ex)
                { }

                Clipboard.Clear();

                Clipboard.SetText(str);
                System.Windows.Forms.SendKeys.SendWait("^v");

                Clipboard.Clear();

                // try restore clipboard
                try
                {
                    var newDob = new DataObject();
                    foreach (var format in data)
                        newDob.SetData(format.Key, format.Value);
                    Clipboard.SetDataObject(newDob);
                }
                catch (System.Exception ex) // i dont give any shit about your uber-important data in clipboard!
                { }

            }
            catch (System.Exception ex) // Clipboard.Clear trow exception sometimes. sometimes dont. but sometimes do
            { }
        }

        internal static void Run(string cmd)
        {
            string str = "";
            var success = GetSelectedText(ref str);
            if (!success && !Popup.ShowDialog(ref str))
                return;

            string output = "";

            try
            {
                cmd = cmd.Trim();
                cmd = cmd.Replace(arg_pattern, str);
                var name = cmd.Split(' ', '\t').Aggregate(
                    (x, y) => 
                        File.Exists(x) ? x : x + " " +  y);
                
                var args = cmd.Substring(name.Length);

                // it can be command, not file. try firs word as command name
                if (!File.Exists(name))
                {
                    name = cmd.Split(' ', '\t').First();
                    args = cmd.Substring(name.Length).Trim();
                }

                Process p = new Process();
                p.StartInfo.FileName = name;
                p.StartInfo.Arguments = args;
                p.StartInfo.UseShellExecute = false;

                if (ProgramSettings.Current.cmdHide)
                    p.StartInfo.CreateNoWindow = true;
                
                if (ProgramSettings.Current.cmdCopyToClipboard || ProgramSettings.Current.cmdPasteToOutput)
                    p.StartInfo.RedirectStandardOutput = true;
                
                p.Start();

                if (ProgramSettings.Current.cmdCopyToClipboard || ProgramSettings.Current.cmdPasteToOutput)
                {
                    output = p.StandardOutput.ReadToEnd();

                    if (ProgramSettings.Current.cmdPasteToOutput)
                        SetSelectedText(output);

                    if (ProgramSettings.Current.cmdCopyToClipboard)
                        Clipboard.SetText(output);

                    if (ProgramSettings.Current.cmdPopResults)
                        MessageBlob.ShowPopup(output);
                }
                
                if(ProgramSettings.Current.cmdWait)
                    p.WaitForExit();
            }
            catch (System.Exception ex)
            {
                MessageBlob.ShowPopup(ex.Message);
            }            
        }

        internal static void Browse(string url)
        {
            string str = "";
            var success = GetSelectedText(ref str);
            if (!success && !Popup.ShowDialog(ref str))
                return;

            try
            {
                url = url.Replace(arg_pattern, str);
                Process.Start(url);
            }
            catch (System.Exception ex)
            {
                MessageBlob.ShowPopup(ex.Message);
            }
        }

        internal static void AppendToFile(string file)
        {
            string str = "";
            var success = GetSelectedText(ref str);
            if (!success && !Popup.ShowDialog(ref str))
                return;

            try
            {
                var fileName = ProgramSettings.Current.appendFile;
                if(fileName == "" || !Uri.IsWellFormedUriString(fileName, UriKind.RelativeOrAbsolute))
                {
                    fileName = "temp.txt";
                    ProgramSettings.Current.appendFile = fileName;
                }

                if(!File.Exists(fileName))
                    using (File.Create(fileName)) { }

                File.AppendAllText(fileName, str + Environment.NewLine);

                MessageBlob.ShowPopup("Text added");
            }
            catch (System.Exception ex)
            {
                MessageBlob.ShowPopup(ex.Message);
            }
        }

        internal static void Translate()
        {
            string str = "";
            var success = GetSelectedText(ref str);
            if (!success && !Popup.ShowDialog(ref str))
                return;

            try
            {
                var pattern = ProgramSettings.Current.translateUrl;
                str = WebUtility.UrlEncode(str);
                var url = string.Format(pattern, ProgramSettings.Current.lngTranslateFrom, ProgramSettings.Current.lngTranslateTo, str);

                BrowserToast.ShowToast(url);
            }
            catch (System.Exception ex)
            {
                MessageBlob.ShowPopup(ex.Message);
            }
        }

        internal static void Regex(string p1, string p2)
        {
            string str = "";
            var success = GetSelectedText(ref str);
            if (!success && !Popup.ShowDialog(ref str))
                return;

            try
            {
                str = System.Text.RegularExpressions.Regex.Replace(str, ProgramSettings.Current.regexFrom, ProgramSettings.Current.regexTo);
                
                if (ProgramSettings.Current.bRegexPasteToOutput)
                    SetSelectedText(str);

                if (ProgramSettings.Current.bRegexCopyToClipboard)
                    Clipboard.SetText(str);

                if (ProgramSettings.Current.bRegexPopResults)
                    MessageBlob.ShowPopup(str);

            }
            catch (System.Exception ex)
            {
                MessageBlob.ShowPopup(ex.Message);
            }
        }

        internal static void ExecuteScript()
        {
            string str = "";
            var success = GetSelectedText(ref str);
            if (!success && !Popup.ShowDialog(ref str))
                return;

            try
            {
                str = ScriptManager.Execute(str);

                if (ProgramSettings.Current.bScriptPasteToOutput)
                    SetSelectedText(str);

                if (ProgramSettings.Current.bScriptCopyToClipboard)
                    Clipboard.SetText(str);

                if (ProgramSettings.Current.bScriptPopResults)
                    MessageBlob.ShowPopup(str);
            }
            catch (System.Exception ex)
            {
                MessageBlob.ShowPopup(ex.Message);
            }
        }
    }
}
