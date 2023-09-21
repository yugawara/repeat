using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace repeat
{
    public enum KeyboardState
    {
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_SYSKEYDOWN = 0x0104,
        WM_SYSKEYUP = 0x0105
    }
    internal static class Program
    {
        private const int WH_KEYBOARD_LL = 13;
        private static IntPtr _keyHookWindowsHandle = IntPtr.Zero;
        private static HookProc _keyHookProc;
        private static IntPtr _user32LibraryHandle = LoadLibrary("User32");

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

        [DllImport("user32.dll")]
        public static extern IntPtr GetKeyboardLayout(uint idThread);


        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct LowLevelKeyboardInputEvent
        {
            public VirtualKeycodes VirtualCode;
            public int HardwareScanCode;
            public int Flags;
            public int TimeStamp;
            public IntPtr AdditionalInformation;
        }

        public enum VirtualKeycodes : int
        {
            // You can fill out this enum with all the necessary VK codes
            // Example:
            VK_ESCAPE = 0x1B,
            VK_ENTER = 0x0D,
            // ... add as needed
        }

        private static void CreateKeyboardHook()
        {
            if (_keyHookWindowsHandle != IntPtr.Zero)
                throw new Exception("There's already a keyboard hook instantiated! No need to create another one.");

            _keyHookProc = (nCode, wParam, lParam) =>
            {
                if (nCode >= 0)
                {
                    var wparamTyped = wParam.ToInt32();
                    if (Enum.IsDefined(typeof(KeyboardState), wparamTyped))
                    {
                        KeyboardState state = (KeyboardState)wparamTyped;

                        LowLevelKeyboardInputEvent inputEvent = (LowLevelKeyboardInputEvent)Marshal.PtrToStructure(lParam, typeof(LowLevelKeyboardInputEvent));

                        byte[] keyboardState = new byte[255];
                        GetKeyboardState(keyboardState);  // Retrieve the current state of the keyboard

                        StringBuilder result = new StringBuilder();
                        ToUnicodeEx((uint)inputEvent.VirtualCode, (uint)inputEvent.HardwareScanCode, keyboardState, result, 5, 0, GetKeyboardLayout(0));

                        string unicodeCharacter = result.ToString();

                        System.Diagnostics.Debug.WriteLine($"Key event detected! VK Code: {inputEvent.VirtualCode}, Hardware Scan Code: {inputEvent.HardwareScanCode}, State: {state}, Character: {unicodeCharacter}");
                    }
                }
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            };


            _keyHookWindowsHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _keyHookProc, _user32LibraryHandle, 0);
            if (_keyHookWindowsHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to adjust keyboard hooks. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }
        }

        private static void RemoveKeyboardHook()
        {
            if (_keyHookWindowsHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyHookWindowsHandle);
                _keyHookWindowsHandle = IntPtr.Zero;
            }
        }

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            if (_user32LibraryHandle == IntPtr.Zero)
            {
                MessageBox.Show("Failed to load User32.dll!");
                return;
            }

            CreateKeyboardHook();

            Form mainForm = new Form1();
            mainForm.FormClosed += (s, e) => RemoveKeyboardHook();  // Unhook when the form is closed

            Application.Run(mainForm);
        }
    }
}
