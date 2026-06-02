using System.Windows.Input;
using EasySave.GUI.Helpers;
using EasySave.Models;

namespace EasySave.GUI.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private string _logFormat;
        private string _businessSoftware;
        private string _encryptedExtensions;
        private string _cryptoSoftPath;

        public string LogFormat
        {
            get => _logFormat;
            set => SetProperty(ref _logFormat, value);
        }
        public string BusinessSoftware
        {
            get => _businessSoftware;
            set => SetProperty(ref _businessSoftware, value);
        }
        public string EncryptedExtensions
        {
            get => _encryptedExtensions;
            set => SetProperty(ref _encryptedExtensions, value);
        }
        public string CryptoSoftPath
        {
            get => _cryptoSoftPath;
            set => SetProperty(ref _cryptoSoftPath, value);
        }

        public bool Saved { get; private set; } = false;

        private readonly System.Action _closeAction;

        public ICommand SaveCommand   { get; }
        public ICommand CancelCommand { get; }

        public SettingsViewModel(AppSettings settings,
                                  System.Action closeAction)
        {
            _closeAction = closeAction;

            LogFormat           = settings.LogFormat;
            BusinessSoftware    = settings.BusinessSoftware;
            CryptoSoftPath      = settings.CryptoSoftPath;

            // Convertir la liste en texte séparé par des virgules
            EncryptedExtensions = string.Join(", ",
                settings.EncryptedExtensions);

            SaveCommand   = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        private void ExecuteSave()
        {
            Saved = true;
            _closeAction?.Invoke();
        }

        private void ExecuteCancel()
        {
            Saved = false;
            _closeAction?.Invoke();
        }

        public AppSettings BuildSettings()
        {
            var extensions = new System.Collections.Generic.List<string>();

            foreach (string ext in EncryptedExtensions.Split(','))
            {
                string trimmed = ext.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    if (!trimmed.StartsWith("."))
                        trimmed = "." + trimmed;
                    extensions.Add(trimmed);
                }
            }

            return new AppSettings
            {
                LogFormat           = LogFormat,
                BusinessSoftware    = BusinessSoftware?.Trim() ?? "",
                EncryptedExtensions = extensions,
                CryptoSoftPath      = CryptoSoftPath?.Trim() ?? ""
            };
        }
    }
}