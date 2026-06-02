using System.Windows;
using EasySave.GUI.ViewModels;
using EasySave.Models;

namespace EasySave.GUI.Views
{
    public partial class SettingsWindow : Window
    {
        private SettingsViewModel _vm;

        public bool         Saved           => _vm.Saved;
        public AppSettings  UpdatedSettings => _vm.BuildSettings();

        public SettingsWindow(AppSettings current)
        {
            InitializeComponent();
            _vm = new SettingsViewModel(current, () => Close());
            DataContext = _vm;
        }
    }
}