using System;
using System.Diagnostics;
using System.IO;

namespace EasySave.Services
{
    public class CryptoSoftAdapter
    {
        private readonly string _cryptoSoftPath;

        public CryptoSoftAdapter(string cryptoSoftPath)
        {
            _cryptoSoftPath = cryptoSoftPath;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_cryptoSoftPath)
            && File.Exists(_cryptoSoftPath);

        public long Encrypt(string filePath)
        {
            if (!IsConfigured)
                return 0;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName        = _cryptoSoftPath,
                    Arguments       = $"\"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow  = true
                };

                Stopwatch sw = Stopwatch.StartNew();

                Process process = Process.Start(psi);
                process.WaitForExit();

                sw.Stop();

                if (process.ExitCode != 0)
                    return -process.ExitCode;

                return sw.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[ERROR] CryptoSoft failed on '{filePath}': {ex.Message}");
                return -1;
            }
        }

        public bool ShouldEncrypt(string filePath,
            System.Collections.Generic.List<string> extensions)
        {
            if (extensions == null || extensions.Count == 0)
                return false;

            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            return extensions.Contains(ext);
        }
    }
}