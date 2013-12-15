using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Runtime;

namespace Vestbi
{
    /// <summary>
    /// SendKeys reverse engineered (without JournalHook routine)
    /// Known bug - original sendkeys is not unicode and is not working with non-english keyboards
    /// </summary>

    public class SendKeysRE
    {
        #region pinvoke

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern bool BlockInput([In, MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern int GetKeyboardState(byte[] keystate);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern int SetKeyboardState(byte[] keystate);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern int OemKeyScan(short wAsciiVal);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

        #endregion

        #region Nested Types

        private class KeywordVk
        {
            // Fields
            internal string keyword;
            internal int vk;

            // Methods
            public KeywordVk(string key, int v)
            {
                this.keyword = key;
                this.vk = v;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public INPUTUNION inputUnion;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            // Fields
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public short wVk;
            public short wScan;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        private class SKEvent
        {
            // Fields
            internal IntPtr hwnd;
            internal int paramH;
            internal int paramL;
            internal int wm;

            // Methods
            public SKEvent(int a, int b, bool c, IntPtr hwnd)
            {
                this.wm = a;
                this.paramL = b;
                this.paramH = c ? 1 : 0;
                this.hwnd = hwnd;
            }

            public SKEvent(int a, int b, int c, IntPtr hwnd)
            {
                this.wm = a;
                this.paramL = b;
                this.paramH = c;
                this.hwnd = hwnd;
            }
        }

        #endregion

        #region Fields

        private static Queue events;
        private static bool fStartNewChar;

        private static bool capslockChanged;
        private static bool kanaChanged;
        private static bool numlockChanged;
        private static bool scrollLockChanged;

        private static KeywordVk[] keywords = new KeywordVk[] { 
            new KeywordVk("ENTER", 13), new KeywordVk("TAB", 9), new KeywordVk("ESC", 0x1b), new KeywordVk("ESCAPE", 0x1b), new KeywordVk("HOME", 0x24), new KeywordVk("END", 0x23), new KeywordVk("LEFT", 0x25), new KeywordVk("RIGHT", 0x27), new KeywordVk("UP", 0x26), new KeywordVk("DOWN", 40), new KeywordVk("PGUP", 0x21), new KeywordVk("PGDN", 0x22), new KeywordVk("NUMLOCK", 0x90), new KeywordVk("SCROLLLOCK", 0x91), new KeywordVk("PRTSC", 0x2c), new KeywordVk("BREAK", 3), 
            new KeywordVk("BACKSPACE", 8), new KeywordVk("BKSP", 8), new KeywordVk("BS", 8), new KeywordVk("CLEAR", 12), new KeywordVk("CAPSLOCK", 20), new KeywordVk("INS", 0x2d), new KeywordVk("INSERT", 0x2d), new KeywordVk("DEL", 0x2e), new KeywordVk("DELETE", 0x2e), new KeywordVk("HELP", 0x2f), new KeywordVk("F1", 0x70), new KeywordVk("F2", 0x71), new KeywordVk("F3", 0x72), new KeywordVk("F4", 0x73), new KeywordVk("F5", 0x74), new KeywordVk("F6", 0x75), 
            new KeywordVk("F7", 0x76), new KeywordVk("F8", 0x77), new KeywordVk("F9", 120), new KeywordVk("F10", 0x79), new KeywordVk("F11", 0x7a), new KeywordVk("F12", 0x7b), new KeywordVk("F13", 0x7c), new KeywordVk("F14", 0x7d), new KeywordVk("F15", 0x7e), new KeywordVk("F16", 0x7f), new KeywordVk("MULTIPLY", 0x6a), new KeywordVk("ADD", 0x6b), new KeywordVk("SUBTRACT", 0x6d), new KeywordVk("DIVIDE", 0x6f), new KeywordVk("+", 0x6b), new KeywordVk("%", 0x10035), 
            new KeywordVk("^", 0x10036)
        };

        #endregion

        private static void AddCancelModifiersForPreviousEvents(Queue previousEvents)
        {
            if (previousEvents != null)
            {
                bool flag = false;
                bool flag2 = false;
                bool flag3 = false;
                while (previousEvents.Count > 0)
                {
                    bool flag4;
                    SKEvent event2 = (SKEvent)previousEvents.Dequeue();
                    if ((event2.wm == 0x101) || (event2.wm == 0x105))
                    {
                        flag4 = false;
                    }
                    else
                    {
                        if ((event2.wm != 0x100) && (event2.wm != 260))
                        {
                            continue;
                        }
                        flag4 = true;
                    }
                    if (event2.paramL == 0x10)
                    {
                        flag = flag4;
                    }
                    else
                    {
                        if (event2.paramL == 0x11)
                        {
                            flag2 = flag4;
                            continue;
                        }
                        if (event2.paramL == 0x12)
                        {
                            flag3 = flag4;
                        }
                    }
                }
                if (flag)
                {
                    AddEvent(new SKEvent(0x101, 0x10, false, IntPtr.Zero));
                }
                else if (flag2)
                {
                    AddEvent(new SKEvent(0x101, 0x11, false, IntPtr.Zero));
                }
                else if (flag3)
                {
                    AddEvent(new SKEvent(0x105, 0x12, false, IntPtr.Zero));
                }
            }
        }

        private static void AddEvent(SKEvent skevent)
        {
            if (events == null)
            {
                events = new Queue();
            }
            events.Enqueue(skevent);
        }

        private static void AddMsgsForVK(int vk, int repeat, bool altnoctrldown, IntPtr hwnd)
        {
            for (int i = 0; i < repeat; i++)
            {
                AddEvent(new SKEvent(altnoctrldown ? 260 : 0x100, vk, fStartNewChar, hwnd));
                AddEvent(new SKEvent(altnoctrldown ? 0x105 : 0x101, vk, fStartNewChar, hwnd));
            }
        }

        private static bool AddSimpleKey(char character, int repeat, IntPtr hwnd, int[] haveKeys, bool fStartNewChar, int cGrp)
        {
            // find propriate keyboard layout
            int num = -1;
            var lngs = System.Windows.Forms.InputLanguage.InstalledInputLanguages;
            for (int i = 0; i < lngs.Count; i++)
            {
                num = VkKeyScanEx(character, lngs[i].Handle);
                if (num != -1)
                    break;
            }

            if (num != -1)
            {
                if ((haveKeys[0] == 0) && ((num & 0x100) != 0))
                {
                    AddEvent(new SKEvent(0x100, 0x10, fStartNewChar, hwnd));
                    fStartNewChar = false;
                    haveKeys[0] = 10;
                }
                if ((haveKeys[1] == 0) && ((num & 0x200) != 0))
                {
                    AddEvent(new SKEvent(0x100, 0x11, fStartNewChar, hwnd));
                    fStartNewChar = false;
                    haveKeys[1] = 10;
                }
                if ((haveKeys[2] == 0) && ((num & 0x400) != 0))
                {
                    AddEvent(new SKEvent(0x100, 0x12, fStartNewChar, hwnd));
                    fStartNewChar = false;
                    haveKeys[2] = 10;
                }
                AddMsgsForVK(num & 0xff, repeat, (haveKeys[2] > 0) && (haveKeys[1] == 0), hwnd);
                CancelMods(haveKeys, 10, hwnd);
            }
            else
            {
                int num2 = OemKeyScan((short)('\x00ff' & character));
                for (int i = 0; i < repeat; i++)
                {
                    AddEvent(new SKEvent(0x102, character, num2 & 0xffff, hwnd));
                }
            }
            if (cGrp != 0)
            {
                fStartNewChar = true;
            }
            return fStartNewChar;
        }

        private static void CancelMods(int[] haveKeys, int level, IntPtr hwnd)
        {
            if (haveKeys[0] == level)
            {
                AddEvent(new SKEvent(0x101, 0x10, false, hwnd));
                haveKeys[0] = 0;
            }
            if (haveKeys[1] == level)
            {
                AddEvent(new SKEvent(0x101, 0x11, false, hwnd));
                haveKeys[1] = 0;
            }
            if (haveKeys[2] == level)
            {
                AddEvent(new SKEvent(0x105, 0x12, false, hwnd));
                haveKeys[2] = 0;
            }
        }

        private static void CheckGlobalKeys(SKEvent skEvent)
        {
            if (skEvent.wm == 0x100)
            {
                switch (skEvent.paramL)
                {
                    case 20:
                        capslockChanged = !capslockChanged;
                        return;

                    case 0x15:
                        kanaChanged = !kanaChanged;
                        break;

                    case 0x90:
                        numlockChanged = !numlockChanged;
                        return;

                    case 0x91:
                        scrollLockChanged = !scrollLockChanged;
                        return;

                    default:
                        return;
                }
            }
        }

        private static void ClearGlobalKeys()
        {
            capslockChanged = false;
            numlockChanged = false;
            scrollLockChanged = false;
            kanaChanged = false;
        }

        private static void ClearKeyboardState()
        {
            byte[] keyboardState = GetKeyboardState();
            keyboardState[20] = 0;
            keyboardState[0x90] = 0;
            keyboardState[0x91] = 0;
            SetKeyboardState(keyboardState);
        }

        public static void Flush()
        {
            System.Windows.Forms.Application.DoEvents();
            while ((events != null) && (events.Count > 0))
            {
                System.Windows.Forms.Application.DoEvents();
            }
        }

        private static byte[] GetKeyboardState()
        {
            byte[] keystate = new byte[0x100];
            GetKeyboardState(keystate);
            return keystate;
        }

        private static bool IsExtendedKey(SKEvent skEvent)
        {
            if (((((skEvent.paramL != 0x26) && (skEvent.paramL != 40)) && ((skEvent.paramL != 0x25) && (skEvent.paramL != 0x27))) && (((skEvent.paramL != 0x21) && (skEvent.paramL != 0x22)) && ((skEvent.paramL != 0x24) && (skEvent.paramL != 0x23)))) && (skEvent.paramL != 0x2d))
            {
                return (skEvent.paramL == 0x2e);
            }
            return true;
        }

        private static int MatchKeyword(string keyword)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (string.Equals(keywords[i].keyword, keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return keywords[i].vk;
                }
            }
            return -1;
        }

        private static void ParseKeys(string keys, IntPtr hwnd)
        {
            int num = 0;
            int[] haveKeys = new int[3];
            int cGrp = 0;
            fStartNewChar = true;
            int length = keys.Length;
            while (num < length)
            {
                int num6;
                int num7;
                int repeat = 1;
                char ch = keys[num];
                int vk = 0;
                switch (ch)
                {
                    case '%':
                        if (haveKeys[2] != 0)
                        {
                            throw new ArgumentException("InvalidSendKeysString");
                        }
                        goto Label_03C9;

                    case '(':
                        cGrp++;
                        if (cGrp > 3)
                        {
                            throw new ArgumentException("SendKeysNestingError");
                        }
                        goto Label_0414;

                    case ')':
                        if (cGrp < 1)
                        {
                            throw new ArgumentException("InvalidSendKeysString");
                        }
                        goto Label_045A;

                    case '+':
                        if (haveKeys[0] != 0)
                        {
                            throw new ArgumentException("InvalidSendKeysString");
                        }
                        goto Label_0333;

                    case '^':
                        if (haveKeys[1] != 0)
                        {
                            throw new ArgumentException("InvalidSendKeysString");
                        }
                        AddEvent(new SKEvent(0x100, 0x11, fStartNewChar, hwnd));
                        fStartNewChar = false;
                        haveKeys[1] = 10;
                        goto Label_04AB;

                    case '{':
                        num6 = num + 1;
                        if (((num6 + 1) >= length) || (keys[num6] != '}'))
                        {
                            goto Label_00EB;
                        }
                        num7 = num6 + 1;
                        goto Label_00C7;

                    case '}':
                        throw new ArgumentException("InvalidSendKeysString");

                    case '~':
                        vk = 13;
                        AddMsgsForVK(vk, repeat, (haveKeys[2] > 0) && (haveKeys[1] == 0), hwnd);
                        goto Label_04AB;

                    default:
                        fStartNewChar = AddSimpleKey(keys[num], repeat, hwnd, haveKeys, fStartNewChar, cGrp);
                        goto Label_04AB;
                }
            Label_00C1:
                num7++;
            Label_00C7:
                if ((num7 < length) && (keys[num7] != '}'))
                {
                    goto Label_00C1;
                }
                if (num7 < length)
                {
                    num6++;
                }
            Label_00EB:
                while (((num6 < length) && (keys[num6] != '}')) && !char.IsWhiteSpace(keys[num6]))
                {
                    num6++;
                }
                if (num6 >= length)
                {
                    throw new ArgumentException("SendKeysKeywordDelimError");
                }
                string keyword = keys.Substring(num + 1, num6 - (num + 1));
                if (char.IsWhiteSpace(keys[num6]))
                {
                    while ((num6 < length) && char.IsWhiteSpace(keys[num6]))
                    {
                        num6++;
                    }
                    if (num6 >= length)
                    {
                        throw new ArgumentException("SendKeysKeywordDelimError");
                    }
                    if (char.IsDigit(keys[num6]))
                    {
                        int startIndex = num6;
                        while ((num6 < length) && char.IsDigit(keys[num6]))
                        {
                            num6++;
                        }
                        repeat = int.Parse(keys.Substring(startIndex, num6 - startIndex), System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                if (num6 >= length)
                {
                    throw new ArgumentException("SendKeysKeywordDelimError");
                }
                if (keys[num6] != '}')
                {
                    throw new ArgumentException("InvalidSendKeysRepeat");
                }
                vk = MatchKeyword(keyword);
                if (vk != -1)
                {
                    if ((haveKeys[0] == 0) && ((vk & 0x10000) != 0))
                    {
                        AddEvent(new SKEvent(0x100, 0x10, fStartNewChar, hwnd));
                        fStartNewChar = false;
                        haveKeys[0] = 10;
                    }
                    if ((haveKeys[1] == 0) && ((vk & 0x20000) != 0))
                    {
                        AddEvent(new SKEvent(0x100, 0x11, fStartNewChar, hwnd));
                        fStartNewChar = false;
                        haveKeys[1] = 10;
                    }
                    if ((haveKeys[2] == 0) && ((vk & 0x40000) != 0))
                    {
                        AddEvent(new SKEvent(0x100, 0x12, fStartNewChar, hwnd));
                        fStartNewChar = false;
                        haveKeys[2] = 10;
                    }
                    AddMsgsForVK(vk, repeat, (haveKeys[2] > 0) && (haveKeys[1] == 0), hwnd);
                    CancelMods(haveKeys, 10, hwnd);
                }
                else
                {
                    if (keyword.Length != 1)
                    {
                        throw new ArgumentException("InvalidSendKeysKeyword");
                    }
                    fStartNewChar = AddSimpleKey(keyword[0], repeat, hwnd, haveKeys, fStartNewChar, cGrp);
                }
                num = num6;
                goto Label_04AB;
            Label_0333:
                AddEvent(new SKEvent(0x100, 0x10, fStartNewChar, hwnd));
                fStartNewChar = false;
                haveKeys[0] = 10;
                goto Label_04AB;
            Label_03C9:
                AddEvent(new SKEvent((haveKeys[1] != 0) ? 0x100 : 260, 0x12, fStartNewChar, hwnd));
                fStartNewChar = false;
                haveKeys[2] = 10;
                goto Label_04AB;
            Label_0414:
                if (haveKeys[0] == 10)
                {
                    haveKeys[0] = cGrp;
                }
                if (haveKeys[1] == 10)
                {
                    haveKeys[1] = cGrp;
                }
                if (haveKeys[2] == 10)
                {
                    haveKeys[2] = cGrp;
                }
                goto Label_04AB;
            Label_045A:
                CancelMods(haveKeys, cGrp, hwnd);
                cGrp--;
                if (cGrp == 0)
                {
                    fStartNewChar = true;
                }
            Label_04AB:
                num++;
            }
            if (cGrp != 0)
            {
                throw new ArgumentException("SendKeysGroupDelimError");
            }
            CancelMods(haveKeys, 10, hwnd);
        }

        private static void ResetKeyboardUsingSendInput(int INPUTSize)
        {
            if ((capslockChanged || numlockChanged) || (scrollLockChanged || kanaChanged))
            {
                INPUT[] pInputs = new INPUT[2];
                pInputs[0].type = 1;
                pInputs[0].inputUnion.ki.dwFlags = 0;
                pInputs[1].type = 1;
                pInputs[1].inputUnion.ki.dwFlags = 2;
                if (capslockChanged)
                {
                    pInputs[0].inputUnion.ki.wVk = 20;
                    pInputs[1].inputUnion.ki.wVk = 20;
                    SendInput(2, pInputs, INPUTSize);
                }
                if (numlockChanged)
                {
                    pInputs[0].inputUnion.ki.wVk = 0x90;
                    pInputs[1].inputUnion.ki.wVk = 0x90;
                    SendInput(2, pInputs, INPUTSize);
                }
                if (scrollLockChanged)
                {
                    pInputs[0].inputUnion.ki.wVk = 0x91;
                    pInputs[1].inputUnion.ki.wVk = 0x91;
                    SendInput(2, pInputs, INPUTSize);
                }
                if (kanaChanged)
                {
                    pInputs[0].inputUnion.ki.wVk = 0x15;
                    pInputs[1].inputUnion.ki.wVk = 0x15;
                    SendInput(2, pInputs, INPUTSize);
                }
            }
        }

        public static void Send(string keys)
        {
            if ((keys != null) && (keys.Length != 0))
            {
                Queue previousEvents = null;
                if ((events != null) && (events.Count != 0))
                {
                    previousEvents = (Queue)events.Clone();
                }
                ParseKeys(keys, IntPtr.Zero);
                if (events != null)
                {
                    byte[] keyboardState = GetKeyboardState();
                    SendInput(keyboardState, previousEvents);
                    Flush();
                }
            }
        }

        private static void SendInput(byte[] oldKeyboardState, Queue previousEvents)
        {
            int count;
            AddCancelModifiersForPreviousEvents(previousEvents);
            INPUT[] pInputs = new INPUT[2];
            pInputs[0].type = 1;
            pInputs[1].type = 1;
            pInputs[1].inputUnion.ki.wVk = 0;
            pInputs[1].inputUnion.ki.dwFlags = 6;
            pInputs[0].inputUnion.ki.dwExtraInfo = IntPtr.Zero;
            pInputs[0].inputUnion.ki.time = 0;
            pInputs[1].inputUnion.ki.dwExtraInfo = IntPtr.Zero;
            pInputs[1].inputUnion.ki.time = 0;
            int cbSize = Marshal.SizeOf(typeof(INPUT));
            uint num2 = 0;
            lock (events.SyncRoot)
            {
                bool flag = BlockInput(true);
                try
                {
                    count = events.Count;
                    ClearGlobalKeys();
                    for (int i = 0; i < count; i++)
                    {
                        SKEvent skEvent = (SKEvent)events.Dequeue();
                        pInputs[0].inputUnion.ki.dwFlags = 0;
                        if (skEvent.wm == 0x102)
                        {
                            pInputs[0].inputUnion.ki.wVk = 0;
                            pInputs[0].inputUnion.ki.wScan = (short)skEvent.paramL;
                            pInputs[0].inputUnion.ki.dwFlags = 4;
                            pInputs[1].inputUnion.ki.wScan = (short)skEvent.paramL;
                            num2 += SendInput(2, pInputs, cbSize) - 1;
                        }
                        else
                        {
                            pInputs[0].inputUnion.ki.wScan = 0;
                            if ((skEvent.wm == 0x101) || (skEvent.wm == 0x105))
                            {
                                pInputs[0].inputUnion.ki.dwFlags |= 2;
                            }
                            if (IsExtendedKey(skEvent))
                            {
                                pInputs[0].inputUnion.ki.dwFlags |= 1;
                            }
                            pInputs[0].inputUnion.ki.wVk = (short)skEvent.paramL;
                            num2 += SendInput(1, pInputs, cbSize);
                            CheckGlobalKeys(skEvent);
                        }
                        Thread.Sleep(1);
                    }
                    ResetKeyboardUsingSendInput(cbSize);
                }
                finally
                {
                    SetKeyboardState(oldKeyboardState);
                    if (flag)
                    {
                        BlockInput(false);
                    }
                }
            }
            if (num2 != count)
            {
                throw new ApplicationException("ups! Something wrong just happened!");
            }
        }
    }
}