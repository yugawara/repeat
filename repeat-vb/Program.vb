Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Windows.Forms

Namespace repeat
    Public Enum KeyboardState
        WM_KEYDOWN = &H100
        WM_KEYUP = &H101
        WM_SYSKEYDOWN = &H104
        WM_SYSKEYUP = &H105
    End Enum

    Friend Module Program
        Private Const WH_KEYBOARD_LL As Integer = 13
        Private _keyHookWindowsHandle As IntPtr = IntPtr.Zero
        Private _keyHookProc As HookProc
        Private _user32LibraryHandle As IntPtr = LoadLibrary("User32")

        Public Delegate Function HookProc(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr

        <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Function LoadLibrary(lpFileName As String) As IntPtr
        End Function

        <DllImport("user32.dll", SetLastError:=True)>
        Public Function GetKeyboardState(lpKeyState() As Byte) As Boolean
        End Function

        <DllImport("user32.dll")>
        Public Function ToUnicodeEx(wVirtKey As UInteger, wScanCode As UInteger, lpKeyState() As Byte, <Out, MarshalAs(UnmanagedType.LPWStr, SizeConst:=64)> pwszBuff As StringBuilder, cchBuff As Integer, wFlags As UInteger, dwhkl As IntPtr) As Integer
        End Function

        <DllImport("user32.dll")>
        Public Function GetKeyboardLayout(idThread As UInteger) As IntPtr
        End Function

        <DllImport("user32.dll", SetLastError:=True)>
        Private Function SetWindowsHookEx(idHook As Integer, lpfn As HookProc, hMod As IntPtr, dwThreadId As UInteger) As IntPtr
        End Function

        <DllImport("user32.dll", SetLastError:=True)>
        Private Function UnhookWindowsHookEx(hhk As IntPtr) As Boolean
        End Function

        <DllImport("user32.dll")>
        Private Function CallNextHookEx(hhk As IntPtr, nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
        End Function

        <StructLayout(LayoutKind.Sequential)>
        Public Structure LowLevelKeyboardInputEvent
            Public VirtualCode As VirtualKeycodes
            Public HardwareScanCode As Integer
            Public Flags As Integer
            Public TimeStamp As Integer
            Public AdditionalInformation As IntPtr
        End Structure

        Public Enum VirtualKeycodes As Integer
            VK_ESCAPE = &H1B
            VK_ENTER = &HD
            ' ... add as needed
        End Enum

        Private Sub CreateKeyboardHook()
            If _keyHookWindowsHandle <> IntPtr.Zero Then
                Throw New Exception("There's already a keyboard hook instantiated! No need to create another one.")
            End If

            _keyHookProc = Function(nCode, wParam, lParam)
                               If nCode >= 0 Then
                                   Dim wparamTyped As Integer = wParam.ToInt32()
                                   If [Enum].IsDefined(GetType(KeyboardState), wparamTyped) Then
                                       Dim state As KeyboardState = CType(wparamTyped, KeyboardState)

                                       Dim inputEvent As LowLevelKeyboardInputEvent = CType(Marshal.PtrToStructure(lParam, GetType(LowLevelKeyboardInputEvent)), LowLevelKeyboardInputEvent)

                                       Dim keyboardState(255) As Byte
                                       GetKeyboardState(keyboardState)  ' Retrieve the current state of the keyboard

                                       Dim result As New StringBuilder()
                                       ToUnicodeEx(CType(inputEvent.VirtualCode, UInteger), CType(inputEvent.HardwareScanCode, UInteger), keyboardState, result, 5, 0, GetKeyboardLayout(0))

                                       Dim unicodeCharacter As String = result.ToString()

                                       System.Diagnostics.Debug.WriteLine($"Key event detected! VK Code: {inputEvent.VirtualCode}, Hardware Scan Code: {inputEvent.HardwareScanCode}, State: {state}, Character: {unicodeCharacter}")
                                   End If
                               End If
                               Return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam)
                           End Function

            _keyHookWindowsHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _keyHookProc, _user32LibraryHandle, 0)
            If _keyHookWindowsHandle = IntPtr.Zero Then
                Dim errorCode As Integer = Marshal.GetLastWin32Error()
                Throw New Win32Exception(errorCode, $"Failed to adjust keyboard hooks. Error {errorCode}: {New Win32Exception(Marshal.GetLastWin32Error()).Message}.")
            End If
        End Sub

        Private Sub RemoveKeyboardHook()
            If _keyHookWindowsHandle <> IntPtr.Zero Then
                UnhookWindowsHookEx(_keyHookWindowsHandle)
                _keyHookWindowsHandle = IntPtr.Zero
            End If
        End Sub

        <STAThread>
        Sub Main()

            Console.Write("AAA")
            ' ApplicationConfiguration.Initialize() ' You might need to provide this or remove it if it's not used in VB.

            If _user32LibraryHandle = IntPtr.Zero Then
                MessageBox.Show("Failed to load User32.dll!")
                Return
            End If

            CreateKeyboardHook()

            Dim mainForm As New Form1()
            AddHandler mainForm.FormClosed, Sub(s, e) RemoveKeyboardHook()  ' Unhook when the form is closed
            Application.Run(mainForm)
        End Sub
    End Module
End Namespace
