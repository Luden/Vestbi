using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.CodeCompletion;
using System.Reflection;
using System.Diagnostics;

namespace Vestbi
{
    class ScriptProvider : ICSharpCode.CodeCompletion.ICSharpScriptProvider
    {
        public string GetUsing()
        {
            return "" +
                "using System; " +
                "using System.Collections.Generic; " +
                "using System.Linq; " +
                "using System.Text; ";
        }


        public string GetVars()
        {
            return null;
        }
    }

    [Serializable]
    public class DomainContext
    {
        public string Request = "";
        public string Result = "";
        public string Error = "";
        public string Path = "";

        public DomainContext(string path, string request)
        {
            Request = request;
            Path = path;
        }
    }

    public class ScriptManager
    {
        public static string Execute(string value)
        {
            // try to load and execute Script assembly
            // then unload appdomain, so assembly file can be deleted and assembly can be rebuilded
            // slow. if performance needed - all domain-creation/type-binding code should be sepparated and called only once

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProgramSettings.Current.scriptAssemblyPath);

            if (!File.Exists(path))
                return null;

            try
            {
                AppDomain domain = AppDomain.CreateDomain("ScriptDomain");
                domain.SetData("context",  new DomainContext(path, value));
                domain.DoCallBack(() => 
                {
                    var dContext = AppDomain.CurrentDomain.GetData("context") as DomainContext;

                    AssemblyName assemblyName = new AssemblyName();
                    assemblyName.CodeBase = dContext.Path;
                    Assembly assembly = AppDomain.CurrentDomain.Load(assemblyName);

                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.GetInterface("Vestbi.IScriptClass") != null)
                        {
                            var script = Activator.CreateInstance(type) as IScriptClass;
                            if (script == null)
                            {
                                dContext.Result = dContext.Request;
                                return;
                            }

                            try
                            {
                                dContext.Result = script.Transform(dContext.Request);
                            }
                            catch (Exception ex)
                            {
                                dContext.Error = ex.Message;
                                dContext.Result = dContext.Request;
                            }
                        }
                    }

                    AppDomain.CurrentDomain.SetData("context", dContext);
                });

                var context = domain.GetData("context") as DomainContext;
                AppDomain.Unload(domain);
                //GC.Collect(); // collects all unused memory
                //GC.WaitForPendingFinalizers(); // wait until GC has finished its work
                //GC.Collect();

                if(context.Error != "")
                    MessageBlob.ShowPopup(context.Error);

                return context.Result;
            }
            catch (Exception ex)
            {
                MessageBlob.ShowPopup(ex.Message);
                return null;
            }
        }

        public static string ParseErrors(string output)
        {
            var crStr = "All rights reserved.";
            var pos = output.IndexOf(crStr);
            if (pos != -1)
            {
                var errLines = new List<string>();
                output = output.Substring(pos + crStr.Length, output.Length - pos - crStr.Length).Trim();

                foreach (var line in output.Split(new char[]{'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, "(.*)\\((\\d+),(\\d+)\\):\\serror\\s.+:\\s(.+)\\[");
                    if (match.Success)
                    {

                        var errLine = string.Format("LINE {0}   {1}", match.Groups[2], match.Groups[4]);
                        errLines.Add(errLine);
                    }
                    if(line.IndexOf("MSBUILD") != -1)
                        errLines.Add(line);
                }

                if (errLines.Count == 0)
                    return "BUILD OK";
                else
                    return string.Join(Environment.NewLine, errLines);                
            }
            return output;
        }

        public static string Compile()
        {
            var p = new Process();
            p.StartInfo.FileName = ProgramSettings.Current.scriptCompilerPath;
            p.StartInfo.Arguments = string.Format(ProgramSettings.Current.scriptCompilerParams,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProgramSettings.Current.scriptProjectPath));
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            return ParseErrors(output);
        }

        public static void InitCodeEditor(CodeTextEditor textEditor)
        {
            var scName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProgramSettings.Current.scriptFile);
            if (!File.Exists(scName))
                using (File.CreateText(scName)) { };

            textEditor.OpenFile(scName);
        }

        public static void SetHighlight(CodeTextEditor textEditor, bool dark)
        {
            var hFile = dark ? @"SyntaxHighlight/CSharp-Mode.xshd" : @"SyntaxHighlight/CSharp-Mode-Light.xshd";

            using (Stream s = File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, hFile)))
            {
                using (XmlTextReader reader = new XmlTextReader(s))
                {
                    var xshd = HighlightingLoader.LoadXshd(reader);
                    textEditor.SyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                }
            }

            textEditor.Completion = new ICSharpCode.CodeCompletion.CSharpCompletion(new ScriptProvider());
        }
    }
}
