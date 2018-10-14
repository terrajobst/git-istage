using System;
using System.Runtime.InteropServices;

namespace GitIStage
{
    internal static class Win32Console
    {
        public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

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
        #pragma warning restore IDE1006 // Naming Styles

        public static void Initialize()
        {
            if (!IsSupported)
                throw new PlatformNotSupportedException();

            var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

            if (GetConsoleMode(iStdOut, out var outConsoleMode))
            {
                outConsoleMode &= ~ENABLE_WRAP_AT_EOL_OUTPUT;
                outConsoleMode |= DISABLE_NEWLINE_AUTO_RETURN;

                SetConsoleMode(iStdOut, outConsoleMode);
            }
        }
    }
}