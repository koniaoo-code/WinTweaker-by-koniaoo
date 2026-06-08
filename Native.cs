using System.Runtime.InteropServices;

namespace WinTweaker.Helpers;

/// <summary>Lightweight native helpers for live system metrics (no extra packages).</summary>
public static class Native
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    /// <summary>Returns (memory load %, total GB, available GB).</summary>
    public static (int Load, double TotalGb, double AvailGb) Memory()
    {
        var m = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref m))
            return ((int)m.dwMemoryLoad, m.ullTotalPhys / 1073741824.0, m.ullAvailPhys / 1073741824.0);
        return (0, 0, 0);
    }
}
