using System;
using System.Runtime.InteropServices;

public static class InputBlocker
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;

    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static LowLevelProc _kbdProc;
    private static LowLevelProc _mouseProc;
    private static IntPtr _kbdHook = IntPtr.Zero;
    private static IntPtr _mouseHook = IntPtr.Zero;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public static void Start()
    {
        if (_kbdHook != IntPtr.Zero || _mouseHook != IntPtr.Zero)
            return;

        _kbdProc = KbdHookProc;
        _mouseProc = MouseHookProc;

        IntPtr hMod = GetModuleHandle(null);
        try
        {
            _kbdHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbdProc, hMod, 0);
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
            Console.WriteLine($"[InputBlocker] Hooks set: kbd={_kbdHook}, mouse={_mouseHook}");
            if (_kbdHook == IntPtr.Zero || _mouseHook == IntPtr.Zero)
            {
                Console.WriteLine("[InputBlocker] Aviso: falha ao setar algum hook (handles Zer0)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InputBlocker] Erro ao setar hooks: {ex.Message}");
        }
    }

    public static void Stop()
    {
        try
        {
            if (_kbdHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_kbdHook);
                _kbdHook = IntPtr.Zero;
            }

            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
        }
        catch { }
    }

    private static IntPtr KbdHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            Console.WriteLine("[InputBlocker] KbdHookProc invoked — consuming event");
            // consume all keyboard events while hooked
            return new IntPtr(1);
        }
        return CallNextHookEx(_kbdHook, nCode, wParam, lParam);
    }

    private static IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            Console.WriteLine("[InputBlocker] MouseHookProc invoked — consuming event");
            // consume all mouse events while hooked
            return new IntPtr(1);
        }
        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }
}
