using System.Windows;
using EasySave.GUI.ViewModels;

namespace EasySave.GUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.OnWindowClosing();
        }
    }
}