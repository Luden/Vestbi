using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ownskit.Utils;

namespace Vestbi
{
    /// <summary>
    /// KeyboardListener wrapper, binds it to textbox
    /// Converts list of key modifiers and key to text and vise versa
    /// Allowed modifiers: ctrl shift alt win 
    /// Allowed only one key
    /// </summary>
    public class Binder
    {
        bool _working = false;

        Dictionary<Key, string> modifierToString = new Dictionary<Key, string>();
        Dictionary<string, Key> stringToModifier = new Dictionary<string, Key>();

        KeyboardListener KListener = new KeyboardListener();
        
        List<Key> _pressedMods = new List<Key>();
        List<Key> _actualMods = new List<Key>();
        List<Key> _beforeMods = new List<Key>();
        Key _actualKey = Key.None;
        Key _beforeKey = Key.None;
        Key _pressedKey = Key.None;

        TextBox _tb;

        public Binder()
        {
            modifierToString[Key.LeftCtrl] = "CTRL";
            modifierToString[Key.RightCtrl] = "CTRL";
            modifierToString[Key.LeftShift] = "SHIFT";
            modifierToString[Key.RightShift] = "SHIFT";
            modifierToString[Key.LeftAlt] = "ALT";
            modifierToString[Key.RightAlt] = "ALT";
            modifierToString[Key.LWin] = "WIN";
            modifierToString[Key.RWin] = "WIN";

            stringToModifier["CTRL"] = Key.LeftCtrl;
            stringToModifier["SHIFT"] = Key.LeftShift;
            stringToModifier["ALT"] = Key.LeftAlt;
            stringToModifier["WIN"] = Key.LWin;
        }

        public bool IsWorking()
        {
            return _working;
        }

        public void Dispose()
        {
            if(KListener != null && !KListener.IsDisposed())
                KListener.Dispose();
        }

        void KListener_KeyDown(object sender, RawKeyEventArgs args)
        {
            if (!_pressedMods.Contains(args.Key) && _pressedKey != args.Key)
            {
                if(!_pressedMods.Any() && _pressedKey == Key.None)
                {
                    switch (args.Key)
                    {
                        case Key.Escape: // rollback and quit
                            Rollback();
                            Stop();
                            return;

                        case Key.Back:
                        case Key.Delete: // clear and quit
                            Clear();
                            Stop();
                            return;

                        case Key.Return: // stop and quit
                            Stop();
                            return;
                    }
                }

                if (modifierToString.ContainsKey(args.Key))
                {
                    _pressedMods.Add(args.Key);
                    _actualMods.Clear();
                    _actualMods.AddRange(_pressedMods);
                }
                else
                {
                    _pressedKey = args.Key;
                    _actualKey = _pressedKey;
                }                
                
                UpdateControls();
            }
        }

        void KListener_KeyUp(object sender, RawKeyEventArgs args)
        {
            if (modifierToString.ContainsKey(args.Key) && _pressedMods.Contains(args.Key))
                _pressedMods.Remove(args.Key);
            else
                _pressedKey = Key.None;
        }

        /// <summary>
        /// Update binded textbox
        /// </summary>
        void UpdateControls()
        {
            if (_tb != null)
            {
                if (!_actualMods.Any() && _actualKey == Key.None)
                {
                    _tb.Text = "Press any key...";
                }
                else
                {
                    _tb.Text = GetKeysStr(_actualMods, _actualKey);
                }
            }
        }

        /// <summary>
        /// Get string representation of list of modifiers and key
        /// </summary>
        /// <param name="mods"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        string GetKeysStr(List<Key> mods, Key key)
        {
            var result = new List<string>();

            foreach (var pair in modifierToString)
            {
                if (mods.Contains(pair.Key) && !result.Contains(pair.Value))
                    result.Add(pair.Value);
            }

            if (key != Key.None)
                result.Add(key.ToString());

            return string.Join(" + ", result);
        }

        /// <summary>
        /// clear current value
        /// </summary>
        void Clear()
        {
            _actualMods.Clear();
            _beforeMods.Clear();
            _actualKey = Key.None;
            _beforeKey = Key.None;
        }

        /// <summary>
        /// rollback to initial value
        /// </summary>
        void Rollback()
        {
            _actualMods.Clear();
            _actualMods.AddRange(_beforeMods);
            _actualKey = _beforeKey;
        }

        /// <summary>
        /// stop listening, unbind textbox
        /// </summary>
        public void Stop()
        {
            if (_working)
            {
                KListener.KeyDown -= KListener_KeyDown;
                KListener.KeyDown -= KListener_KeyUp;

                _working = false;
                _beforeMods.Clear();
                _beforeMods.AddRange(_actualMods);
                _beforeKey = _actualKey;

                UpdateControls();

                if (_tb != null)
                {
                    if (_actualKey == Key.None)
                        _tb.Text = "";
                    _tb.BorderThickness = new Thickness(1, 1, 1, 1);
                    _tb = null;
                }
            }

            if (!KListener.IsDisposed())
                KListener.Dispose();
        }

        /// <summary>
        /// Start listening, bind to textbox
        /// </summary>
        /// <param name="tb"></param>
        public void Start(TextBox tb)
        {
            UpdateControls();

            KListener = new KeyboardListener();
            KListener.KeyDown += new RawKeyEventHandler(KListener_KeyDown);
            KListener.KeyUp += new RawKeyEventHandler(KListener_KeyUp);

            _tb = tb;
            _tb.BorderThickness = new Thickness(3, 3, 3, 3);
            _beforeKey = GetKeylistFromText(_tb.Text, _beforeMods);
            _pressedMods.Clear();
            _actualMods.Clear();
            _actualKey = _beforeKey;
            _actualMods.AddRange(_beforeMods);
            _pressedKey = Key.None;

            _working = true;
        }

        /// <summary>
        /// Converts string to list of modifiers and key
        /// </summary>
        /// <param name="text"></param>
        /// <param name="mods"></param>
        /// <returns></returns>
        public Key GetKeylistFromText(string text, List<Key> mods)
        {
            mods.Clear();

            text = text.Trim();
            if (text == "")
                return Key.None;

            try
            {
                var list = text.Split('+').Select(x => x.Trim());
                mods.AddRange(list.Where(x => stringToModifier.ContainsKey(x)).Select(x => stringToModifier[x]));

                var strRes = list.Where(x => !stringToModifier.ContainsKey(x)).FirstOrDefault();
                if(strRes == "")
                    return Key.None;
                else return (Key)Enum.Parse(typeof(Key), strRes);
            }
            catch (System.Exception ex)
            {
                return Key.None;
            }
        }
    }
}
