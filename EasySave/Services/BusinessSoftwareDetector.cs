using System.Diagnostics;

namespace EasySave.Services
{
    public static class BusinessSoftwareDetector
    {
        public static bool IsRunning(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return false;

            Process[] processes =
                Process.GetProcessesByName(processName);

            return processes.Length > 0;
        }
    }
}