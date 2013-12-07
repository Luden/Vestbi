using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace Vestbi
{
    [Serializable]
    [XmlRoot("Settings")]
    public class ProgramSettings
    {
        public string Theme;
        public string Accent;
        public bool Autostart;
        public bool Minimized;
        public bool GuideShown;

        [XmlElement(ElementName = "Browse_key")]
        public string kBrowse;
        [XmlElement(ElementName = "Browse_url")]
        public string url;
        [XmlElement(ElementName = "Browse_encode_url")]
        public bool bEncodeUrl;

        [XmlElement(ElementName = "Translate_key")]
        public string kTranslate;
        [XmlElement(ElementName = "Translate_from")]
        public string lngTranslateFrom;
        [XmlElement(ElementName = "Translate_to")]
        public string lngTranslateTo;
        [XmlElement(ElementName = "Translate_url_pattern")]
        public string translateUrl;


        [XmlElement(ElementName = "Regex_key")]
        public string kRegex;
        [XmlElement(ElementName = "Regex_search_pattern")]
        public string regexFrom;
        [XmlElement(ElementName = "Regex_replace_pattern")]
        public string regexTo;
        [XmlElement(ElementName = "Regex_copy_to_clipboard")]
        public bool bRegexCopyToClipboard;
        [XmlElement(ElementName = "Regex_paste_to_output")]
        public bool bRegexPasteToOutput;
        [XmlElement(ElementName = "Regex_pop_results")]
        public bool bRegexPopResults;

        [XmlElement(ElementName = "Append_file_name")]
        public string appendFile;
        [XmlElement(ElementName = "Append_key")]
        public string kAppend;

        [XmlElement(ElementName = "Run_key")]
        public string kRun;
        [XmlElement(ElementName = "Run_Command_line")]
        public string cmd;
        [XmlElement(ElementName = "Run_hide_window")]
        public bool cmdHide;
        [XmlElement(ElementName = "Run_wait_completion")]
        public bool cmdWait;
        [XmlElement(ElementName = "Run_copy_results_to_clipboard")]
        public bool cmdCopyToClipboard;
        [XmlElement(ElementName = "Run_paste_results_to_output")]
        public bool cmdPasteToOutput;
        [XmlElement(ElementName = "Run_pop_results")]
        public bool cmdPopResults;

        [XmlElement(ElementName = "Script_key")]
        public string kScript;
        [XmlElement(ElementName = "Script_file_name")]
        public string scriptFile;
        [XmlElement(ElementName = "Script_copy_to_clipboard")]
        public bool bScriptCopyToClipboard;
        [XmlElement(ElementName = "Script_paste_to_output")]
        public bool bScriptPasteToOutput;
        [XmlElement(ElementName = "Script_pop_results")]
        public bool bScriptPopResults;
        [XmlElement(ElementName = "Script_compiler_path")]
        public string scriptCompilerPath;
        [XmlElement(ElementName = "Script_project_path")]
        public string scriptProjectPath;
        [XmlElement(ElementName = "Script_assembly_path")]
        public string scriptAssemblyPath;
        [XmlElement(ElementName = "Script_compiler_params")]
        public string scriptCompilerParams;

        [XmlIgnore]
        public static ProgramSettings Current = new ProgramSettings();

        [XmlIgnore]
        private static string settingsPath = "settings.cfg";
        [XmlIgnore]
        private static string SettingsFullPath
        {
            get 
            {
                return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, settingsPath);
            }
        }

        public static void Save()
        {
            try
            {
                XmlSerializer s = new XmlSerializer(typeof(ProgramSettings));
                using (var writer = new StreamWriter(SettingsFullPath))
                    s.Serialize(writer, Current);
            }
            catch (System.Exception ex)
            { }
        }

        public static void Load()
        {
            try
            {
                // defaults
                if (!File.Exists(SettingsFullPath))
                {
                    using (File.Create(SettingsFullPath)) { };
                    Current.Accent = "Blue";
                    Current.Theme = "Dark";
                    Current.Autostart = false;
                    Current.Minimized = false;
                    Current.GuideShown = false;
                    Current.appendFile = "log.txt";
                    Current.bEncodeUrl = true;
                    Current.cmd = "notepad log.txt";
                    Current.cmdCopyToClipboard = false;
                    Current.cmdPasteToOutput = false;
                    Current.cmdPopResults = false;
                    Current.cmdWait = false;
                    Current.cmdHide = false;
                    Current.lngTranslateFrom = "en";
                    Current.lngTranslateTo = "ru";
                    Current.regexFrom = "#(\\d+)\\:\\s(.*)";
                    Current.regexTo = "$1: $2 - fixed";
                    Current.bRegexCopyToClipboard = true;
                    Current.bRegexPasteToOutput = false;
                    Current.bRegexPopResults = false;
                    Current.url = "http://www.google.com/search?q={text}&ie=utf-8&oe=utf-8&channel=suggest";
                    Current.translateUrl = "http://translate.google.com/?hl=en&tab=wT&authuser=0#{0}/{1}/{2}";
                    Current.scriptFile = "Script/code.cs";
                    Current.scriptCompilerPath = Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "Microsoft.NET\\Framework\\v4.0.30319\\MSBuild.exe");
                    Current.scriptCompilerParams = "\"{0}\" /p:Configuration=Release /p:Platform=\"AnyCPU\" /v:q /clp:ErrorsOnly /p:OutputDir=\"BuildTemp\\\" ";
                    Current.scriptProjectPath = "Script/script.csproj";
                    Current.scriptAssemblyPath = "Script/script.dll";
                    Current.bScriptCopyToClipboard = true;
                    Current.bScriptPasteToOutput = true;
                    Current.bScriptPopResults = true;
                    Save();
                    MessageBlob.ShowPopup("Settings file restored");
                }

                if (File.Exists(SettingsFullPath))
                {
                    XmlSerializer s = new XmlSerializer(typeof(ProgramSettings));
                    using (var reader = new StreamReader(SettingsFullPath))
                        Current = (ProgramSettings)s.Deserialize(reader);
                }
            }
            catch (System.Exception ex)
            {
                MessageBlob.ShowPopup("Settings file corrupted");
            }
        }
    }
}
