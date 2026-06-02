using System.Collections.Generic;

namespace EasySave.Models
{
    public class AppSettings
    {
        public string       LogFormat            { get; set; }
        public string       BusinessSoftware     { get; set; }
        public List<string> EncryptedExtensions  { get; set; }
        public string       CryptoSoftPath       { get; set; }
        
        public List<string> PriorityExtensions   { get; set; }
        public long         MaxLargeFileSizeKb   { get; set; }

        public AppSettings()
        {
            LogFormat           = "json";
            BusinessSoftware    = "";
            EncryptedExtensions = new List<string>();
            CryptoSoftPath      = "";

            // v3.0 : valeurs par défaut
            PriorityExtensions  = new List<string>();
            MaxLargeFileSizeKb  = 1024; 
        }
    }
}