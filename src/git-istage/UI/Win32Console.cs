using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GitIStage.UI;

[SupportedOSPlatform("windows")]
internal static class Win32Console
{
    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

#pragma warning disable IDE1006 // Naming Styles
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
    private const uint ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
#pragma warning restore IDE1006 // Naming Styles

    private static bool _initialized;
    private static uint _originalConsoleMode;
    
    public static void Initialize()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

        if (!_initialized)
        {
            if (!GetConsoleMode(iStdOut, out _originalConsoleMode))
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            _initialized = true;
        }

        var newConsoleMode = _originalConsoleMode;
        newConsoleMode &= ~ENABLE_WRAP_AT_EOL_OUTPUT;
        newConsoleMode |= DISABLE_NEWLINE_AUTO_RETURN | ENABLE_VIRTUAL_TERMINAL_PROCESSING;

        if (!SetConsoleMode(iStdOut, newConsoleMode))
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
    }
    
    public static void Restore()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        if (!_initialized)
            return;

        var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
        if (!SetConsoleMode(iStdOut, _originalConsoleMode))
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
    }
}