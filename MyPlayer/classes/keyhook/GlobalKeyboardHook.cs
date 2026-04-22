using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Serilog;

namespace MyPlayer.classes.keyhook
{
    internal static class GlobalKeyboardHook
    {
        private static Action<Key>? _handleKeyPress = null;
        private static IntPtr _hookID = IntPtr.Zero;
        private static LowLevelKeyboardProc? _proc;

        public static void SetHook(Action<Key> handleKeyPress)
        {
            _proc = HookCallback;
            _hookID = SetWindowsHook(_proc);
            _handleKeyPress = handleKeyPress;

            if (_hookID == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Log.Error("Falha ao definir GlobalKeyboardHook. Erro Win32: {ErrorCode}", errorCode);
            }
            else
            {
                Log.Information("GlobalKeyboardHook definido com sucesso. ID: {HookID}", _hookID);
            }
        }

        public static void Unhook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                Log.Information("GlobalKeyboardHook removido");
            }
        }

        private static IntPtr SetWindowsHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule == null) return IntPtr.Zero;

                // Para WH_KEYBOARD_LL, GetModuleHandle(null) é o mais recomendado no mesmo processo
                IntPtr hMod = GetModuleHandle(null);
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, hMod, 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                
                // Log para debug (pode ser ruidoso, mas ajuda a confirmar se o hook está disparando)
                // Log.Verbose("Tecla capturada via Hook Global: {Key} (VK: {VK})", key, vkCode);

                if (IsMediaKey(key))
                {
                    Log.Information("Tecla de mídia detectada globalmente: {Key}", key);
                    _handleKeyPress?.Invoke(key);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static bool IsMediaKey(Key key)
        {
            return key == Key.MediaNextTrack || 
                   key == Key.MediaPreviousTrack || 
                   key == Key.MediaPlayPause || 
                   key == Key.MediaStop ||
                   key == Key.Play ||
                   key == Key.Pause;
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);
    }
}
