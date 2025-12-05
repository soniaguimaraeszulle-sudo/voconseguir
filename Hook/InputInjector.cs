using System;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Biblioteca para injeção de entrada de teclado e mouse via Windows API
/// Suporta digitação, teclas especiais, hotkeys e controle de mouse
/// </summary>
public class InputInjector
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
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT Point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private const uint GA_ROOT = 2;
    private const int SW_RESTORE = 9;

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

    /// <summary>
    /// Digita um texto via teclado remoto
    /// Suporta maiúsculas, números e caracteres especiais
    /// </summary>
    public static void TextEntry(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // tentar focar a janela sob o cursor para garantir que a aplicação receba entrada
        try { EnsureForegroundWindowUnderCursor(); } catch { }

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
                System.Threading.Thread.Sleep(1);
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

            System.Threading.Thread.Sleep(3);
        }
    }

    /// <summary>
    /// Pressiona uma tecla especial ou executa uma hotkey
    /// Exemplos: "ENTER", "ESCAPE", "F5", "CTRL+C", "ALT+TAB"
    /// </summary>
    public static void KeyPress(string payload)
    {
        if (string.IsNullOrEmpty(payload))
            return;

        try { Console.WriteLine($"[HOOK] KeyPress payload={payload}"); } catch { }

        // garantir foco na janela sob o cursor antes de enviar hotkeys
        try { EnsureForegroundWindowUnderCursor(); } catch { }

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
            // fallback específico para BACKSPACE: muito mais agressivo
            if (vkCode == 0x08)
            {
                // BACKSPACE: estratégia multi-camada com delays muito maiores
                // Primeira tentativa: SendInput com grandes delays
                try { Console.WriteLine($"[HOOK] BACKSPACE: tentativa 1 - SendInput with long delays at {DateTime.Now:HH:mm:ss.fff}"); } catch { }
                
                INPUT[] inputs = new INPUT[2];
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].u.ki.wVk = (ushort)vkCode;
                inputs[0].u.ki.wScan = 0;
                inputs[0].u.ki.dwFlags = 0;
                inputs[0].u.ki.time = 0;
                inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].u.ki.wVk = (ushort)vkCode;
                inputs[1].u.ki.wScan = 0;
                inputs[1].u.ki.dwFlags = KEYEVENTF_KEYUP;
                inputs[1].u.ki.time = 0;
                inputs[1].u.ki.dwExtraInfo = IntPtr.Zero;

                SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
                System.Threading.Thread.Sleep(100); // delay muito maior entre SendInput
                
                // Segunda tentativa: keybd_event com delays ainda maiores
                try { Console.WriteLine($"[HOOK] BACKSPACE: tentativa 2 - keybd_event fallback at {DateTime.Now:HH:mm:ss.fff}"); } catch { }
                keybd_event(0x08, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(150);
                keybd_event(0x08, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                System.Threading.Thread.Sleep(50);
            }
            else
            {
                SendKeyInput(vkCode, 0);
                // pequeno delay entre down e up para melhorar confiabilidade
                System.Threading.Thread.Sleep(30);
                SendKeyInput(vkCode, KEYEVENTF_KEYUP);
            }
        }
    }

    /// <summary>
    /// Executa uma hotkey (combinação de teclas)
    /// Exemplos: "CTRL+C", "ALT+TAB", "SHIFT+F5"
    /// </summary>
    private static void HandleHotkey(string hotkey)
    {
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
        // garantir foco
        try { EnsureForegroundWindowUnderCursor(); } catch { }
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

    // Tenta trazer para foreground a janela que está sob o cursor do mouse
    private static void EnsureForegroundWindowUnderCursor()
    {
        try
        {
            if (!GetCursorPos(out var p)) return;

            try { Console.WriteLine($"[HOOK] EnsureForeground: cursor=({p.X},{p.Y})"); } catch { }

            IntPtr hwnd = WindowFromPoint(p);
            if (hwnd == IntPtr.Zero) return;

            IntPtr top = GetAncestor(hwnd, GA_ROOT);
            if (top == IntPtr.Zero) top = hwnd;

            IntPtr fg = GetForegroundWindow();
            try { Console.WriteLine($"[HOOK] EnsureForeground: hwndUnderCursor=0x{top.ToInt64():X}, foreground=0x{fg.ToInt64():X}"); } catch { }
            if (fg == top) return;

            uint fgThread = GetWindowThreadProcessId(fg, out _);
            uint targetThread = GetWindowThreadProcessId(top, out _);
            uint currentThread = GetCurrentThreadId();

            // Anexa threads para permitir SetForegroundWindow
            AttachThreadInput(currentThread, fgThread, true);
            AttachThreadInput(currentThread, targetThread, true);

            // Muito mais agressivo: tentar múltiplas vezes com delays
            for (int i = 0; i < 3; i++)
            {
                ShowWindow(top, SW_RESTORE);
                SetForegroundWindow(top);
                BringWindowToTop(top);
                System.Threading.Thread.Sleep(50);
            }

            try { Console.WriteLine($"[HOOK] EnsureForeground: SetForegroundWindow called 3x for hwnd=0x{top.ToInt64():X}"); } catch { }
            AttachThreadInput(currentThread, fgThread, false);
            AttachThreadInput(currentThread, targetThread, false);
        }
        catch (Exception ex)
        {
            try { Console.WriteLine($"[HOOK] EnsureForeground exception: {ex.Message}"); } catch { }
        }
    }

    /// <summary>
    /// Clique esquerdo do mouse na posição atual
    /// </summary>
    public static void LeftClick()
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }

    /// <summary>
    /// Clique direito do mouse na posição atual
    /// </summary>
    public static void RightClick()
    {
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
    }

    /// <summary>
    /// Move o cursor do mouse para uma coordenada absoluta
    /// </summary>
    public static void MoveMouseTo(int x, int y)
    {
        SetCursorPos(x, y);
    }

    /// <summary>
    /// Envia um comando de entrada genérico
    /// type: "TEXT", "KEY_PRESS", "MOUSE_MOVE", "MOUSE_LEFT_CLICK", "MOUSE_RIGHT_CLICK"
    /// payload: dados do comando (texto para TEXT, código para KEY_PRESS, "x;y" para MOUSE_MOVE)
    /// </summary>
    public static void ExecuteCommand(string type, string payload)
    {
        if (string.IsNullOrEmpty(type))
            return;

        switch (type.ToUpper())
        {
            case "TEXT":
                TextEntry(payload);
                break;

            case "KEY_PRESS":
                KeyPress(payload);
                break;

            case "MOUSE_MOVE":
                if (!string.IsNullOrEmpty(payload))
                {
                    var parts = payload.Split(';');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int x) &&
                        int.TryParse(parts[1], out int y))
                    {
                        MoveMouseTo(x, y);
                    }
                }
                break;

            case "MOUSE_LEFT_CLICK":
                LeftClick();
                break;

            case "MOUSE_RIGHT_CLICK":
                RightClick();
                break;
        }
    }

    // Métodos auxiliares
    private static void SendKeyInput(ushort vkCode, uint flags)
    {
        INPUT[] inputs = new INPUT[1];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = vkCode;
        inputs[0].u.ki.dwFlags = flags;
        inputs[0].u.ki.time = 0;
        inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }
}
