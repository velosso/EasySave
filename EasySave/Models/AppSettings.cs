using System.Collections.Generic;

namespace EasySave.Models
{
    public class AppSettings
    {
        public string       LogFormat            { get; set; }
        public string       BusinessSoftware     { get; set; }
        public List<string> EncryptedExtensions  { get; set; }
        public string       CryptoSoftPath       { get; set; }

        public AppSettings()
        {
            LogFormat           = "json";
            BusinessSoftware    = "";
            EncryptedExtensions = new List<string>();
            CryptoSoftPath      = "";
        }
    }
}