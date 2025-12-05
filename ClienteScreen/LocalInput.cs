using System;
using System.Runtime.InteropServices;
using System.Text;

public class LocalInput
{
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    [DllImport("user32.dll")]
    private static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState, StringBuilder receivedUnicodeChar, int sizeOfBuffer, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentProcessId();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_SCANCODE = 0x0008;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public void TextEntry(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        foreach (char c in text)
        {
            // Usar VkKeyScan para obter o virtual key e shift state
            short keyInfo = VkKeyScan(c);
            
            if (keyInfo == -1)
            {
                // Se VkKeyScan falhar, usar Unicode
                INPUT[] inputs = new INPUT[2];
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].u.ki.wVk = 0;
                inputs[0].u.ki.wScan = (ushort)c;
                inputs[0].u.ki.dwFlags = KEYEVENTF_UNICODE;
                inputs[0].u.ki.time = 0;
                inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].u.ki.wVk = 0;
                inputs[1].u.ki.wScan = (ushort)c;
                inputs[1].u.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
                inputs[1].u.ki.time = 0;
                inputs[1].u.ki.dwExtraInfo = IntPtr.Zero;

                SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
            }
            else
            {
                byte vk = (byte)(keyInfo & 0xFF);
                byte shift = (byte)((keyInfo >> 8) & 0xFF);

                // Se precisa Shift, pressiona antes
                if ((shift & 0x01) != 0) // Shift bit
                {
                    keybd_event(0x10, 0, 0, UIntPtr.Zero); // VK_SHIFT down
                }
                if ((shift & 0x02) != 0) // Ctrl bit
                {
                    keybd_event(0x11, 0, 0, UIntPtr.Zero); // VK_CTRL down
                }
                if ((shift & 0x04) != 0) // Alt bit
                {
                    keybd_event(0x12, 0, 0, UIntPtr.Zero); // VK_ALT down
                }

                // Pressiona a tecla
                keybd_event(vk, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(1); // Reduzido de 2 para velocidade
                keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                // Solta modificadores
                if ((shift & 0x04) != 0)
                {
                    keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
                if ((shift & 0x02) != 0)
                {
                    keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
                if ((shift & 0x01) != 0)
                {
                    keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
            }

            System.Threading.Thread.Sleep(3); // Reduzido de 10 para digitação mais rápida
        }
    }

    public void KeyPress(string payload)
    {
        if (string.IsNullOrEmpty(payload))
            return;

        // Verificar se é uma hotkey com modificadores (Ctrl+X, Alt+Tab, etc)
        if (payload.Contains("+"))
        {
            HandleHotkey(payload);
            return;
        }

        // Mapear nomes de teclas para códigos virtuais
        ushort vkCode = 0;
        switch (payload.ToUpper())
        {
            case "ENTER":
            case "RETURN":
                vkCode = 0x0D; // VK_RETURN
                break;
            case "ESCAPE":
                vkCode = 0x1B; // VK_ESCAPE
                break;
            case "TAB":
                vkCode = 0x09; // VK_TAB
                break;
            case "BACKSPACE":
                vkCode = 0x08; // VK_BACK
                break;
            case "DELETE":
                vkCode = 0x2E; // VK_DELETE
                break;
            case "INSERT":
                vkCode = 0x2D; // VK_INSERT
                break;
            case "HOME":
                vkCode = 0x24; // VK_HOME
                break;
            case "END":
                vkCode = 0x23; // VK_END
                break;
            case "PAGEUP":
                vkCode = 0x21; // VK_PRIOR
                break;
            case "PAGEDOWN":
                vkCode = 0x22; // VK_NEXT
                break;
            case "SPACE":
                vkCode = 0x20; // VK_SPACE
                break;
            case "UP":
                vkCode = 0x26; // VK_UP
                break;
            case "DOWN":
                vkCode = 0x28; // VK_DOWN
                break;
            case "LEFT":
                vkCode = 0x25; // VK_LEFT
                break;
            case "RIGHT":
                vkCode = 0x27; // VK_RIGHT
                break;
            case "PRINTSCREEN":
                vkCode = 0x2C; // VK_SNAPSHOT
                break;
            case "F1":
                vkCode = 0x70; // VK_F1
                break;
            case "F2":
                vkCode = 0x71; // VK_F2
                break;
            case "F3":
                vkCode = 0x72; // VK_F3
                break;
            case "F4":
                vkCode = 0x73; // VK_F4
                break;
            case "F5":
                vkCode = 0x74; // VK_F5
                break;
            case "F6":
                vkCode = 0x75; // VK_F6
                break;
            case "F7":
                vkCode = 0x76; // VK_F7
                break;
            case "F8":
                vkCode = 0x77; // VK_F8
                break;
            case "F9":
                vkCode = 0x78; // VK_F9
                break;
            case "F10":
                vkCode = 0x79; // VK_F10
                break;
            case "F11":
                vkCode = 0x7A; // VK_F11
                break;
            case "F12":
                vkCode = 0x7B; // VK_F12
                break;
            case "CTRL":
            case "CONTROL":
                vkCode = 0x11; // VK_CONTROL
                break;
            case "SHIFT":
                vkCode = 0x10; // VK_SHIFT
                break;
            case "ALT":
                vkCode = 0x12; // VK_MENU
                break;
            case "LWIN":
            case "WINDOWS":
                vkCode = 0x5B; // VK_LWIN
                break;
            case "RWIN":
                vkCode = 0x5C; // VK_RWIN
                break;
            default:
                // Tentar usar o primeiro caractere
                if (payload.Length > 0)
                {
                    short vk = VkKeyScan(payload[0]);
                    if (vk != -1)
                    {
                        vkCode = (ushort)(vk & 0xFF);
                    }
                }
                break;
        }

        if (vkCode != 0)
        {
            SendKeyInput(vkCode, 0);
            SendKeyInput(vkCode, KEYEVENTF_KEYUP);
        }
    }

    private void HandleHotkey(string hotkey)
    {
        // Exemplo: "CTRL+C", "ALT+TAB", "SHIFT+F5"
        var parts = hotkey.Split('+');
        bool ctrl = false, shift = false, alt = false;
        ushort vkCode = 0;

        foreach (var part in parts)
        {
            string key = part.Trim().ToUpper();
            if (key == "CTRL" || key == "CONTROL")
                ctrl = true;
            else if (key == "SHIFT")
                shift = true;
            else if (key == "ALT")
                alt = true;
            else
            {
                // É a tecla principal
                switch (key)
                {
                    case "C": vkCode = 0x43; break;
                    case "V": vkCode = 0x56; break;
                    case "X": vkCode = 0x58; break;
                    case "Z": vkCode = 0x5A; break;
                    case "A": vkCode = 0x41; break;
                    case "S": vkCode = 0x53; break;
                    case "TAB": vkCode = 0x09; break;
                    case "ESCAPE": vkCode = 0x1B; break;
                    case "DELETE": vkCode = 0x2E; break;
                    case "F5": vkCode = 0x74; break;
                    default:
                        short vk = VkKeyScan(key.Length > 0 ? key[0] : ' ');
                        if (vk != -1)
                            vkCode = (ushort)(vk & 0xFF);
                        break;
                }
            }
        }

        if (vkCode == 0)
            return;

        // Pressiona modificadores
        if (ctrl)
            keybd_event(0x11, 0, 0, UIntPtr.Zero);
        if (shift)
            keybd_event(0x10, 0, 0, UIntPtr.Zero);
        if (alt)
            keybd_event(0x12, 0, 0, UIntPtr.Zero);

        // Pressiona tecla principal
        keybd_event((byte)vkCode, 0, 0, UIntPtr.Zero);
        System.Threading.Thread.Sleep(50);
        keybd_event((byte)vkCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        // Solta modificadores
        if (alt)
            keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        if (shift)
            keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        if (ctrl)
            keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private void SendKeyInput(ushort vkCode, uint flags)
    {
        INPUT[] inputs = new INPUT[1];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = vkCode;
        inputs[0].u.ki.dwFlags = flags;
        inputs[0].u.ki.time = 0;
        inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    public void LeftClick()
    {
        // envia click na posição atual
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }

    public void RightClick()
    {
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
    }

    public void MoveMouseTo(int x, int y)
    {
        // posiciona o cursor na coordenada absoluta da tela
        SetCursorPos(x, y);
    }
}
