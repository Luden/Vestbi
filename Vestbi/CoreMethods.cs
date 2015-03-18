using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft;

namespace Vestbi
{
    class CoreMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetOpenClipboardWindow();

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

                SendKeysRE.Send("^c");

                System.Threading.Thread.Sleep(100); // naive attemption to wait for app to response ctrl+c

                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        if (GetOpenClipboardWindow() != IntPtr.Zero) // 100500th multithreading issue. some apps just open clipboard and leave the party...
                        {
                            System.Threading.Thread.Sleep(100);
                            continue;
                        }

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

                Clipboard.SetDataObject(str);

                SendKeysRE.Send("^v");

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
            if (!success && (ProgramSettings.Current.cmdAsk && !Popup.ShowDialog(ref str)))
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
                p.StartInfo.StandardOutputEncoding = Encoding.GetEncoding("CP866");

                if (ProgramSettings.Current.cmdHide)
                    p.StartInfo.CreateNoWindow = true;

                if (ProgramSettings.Current.cmdCopyToClipboard || ProgramSettings.Current.cmdPasteToOutput || ProgramSettings.Current.cmdPopResults)
                    p.StartInfo.RedirectStandardOutput = true;
                
                p.Start();

                if (ProgramSettings.Current.cmdCopyToClipboard || ProgramSettings.Current.cmdPasteToOutput || ProgramSettings.Current.cmdPopResults)
                {
                    output = p.StandardOutput.ReadToEnd();

                    if (ProgramSettings.Current.cmdPasteToOutput)
                        SetSelectedText(output);

                    if (ProgramSettings.Current.cmdCopyToClipboard)
                        Clipboard.SetDataObject(output);

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
                if (ProgramSettings.Current.appendFile == "" || ProgramSettings.Current.appendFile.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                {
                    ProgramSettings.Current.appendFile = "temp.txt";
                }
                
                var fileName = ProgramSettings.Current.appendFile;
                if (!Path.IsPathRooted(fileName))
                    fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                if(!File.Exists(fileName))
                    using (File.Create(fileName)) { }

                if (ProgramSettings.Current.appendTimestamp)
                    str = DateTime.Now.ToString(ProgramSettings.Current.appendTimestampFormat) + Environment.NewLine + str;

                if (ProgramSettings.Current.appendDelimeter)
                    str = ProgramSettings.Current.appendDelimeterFormat.Replace("\\n", Environment.NewLine) + str;

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
				string SECURE_ACCOUNT_ID = "kBcDV52crVLxzkY8rXXO3VyRn+/P2hIrLPGw+cGQaBM=";
				string ROOT_SERVICE_URL = "https://api.datamarket.azure.com/Bing/MicrosoftTranslator/v1/Translate";

				TranslatorContainer container = new TranslatorContainer(new Uri(ROOT_SERVICE_URL));
				container.Credentials = new NetworkCredential("accountKey", SECURE_ACCOUNT_ID);

				DataServiceQuery<Translation> query = container.Translate(str, ProgramSettings.Current.lngTranslateTo, ProgramSettings.Current.lngTranslateFrom);
				foreach (var trans in query.Execute())
				{
					MessageBlob.ShowPopup(trans.Text);
				}
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
                    Clipboard.SetDataObject(str);

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
            if (!success && (ProgramSettings.Current.bScriptAsk && !Popup.ShowDialog(ref str)))
                return;

            try
            {
                str = ScriptManager.Execute(str);

                if (ProgramSettings.Current.bScriptPasteToOutput)
                    SetSelectedText(str);

                if (ProgramSettings.Current.bScriptCopyToClipboard)
                    Clipboard.SetDataObject(str);

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
