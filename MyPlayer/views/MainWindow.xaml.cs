using System.Windows;
using System.Windows.Input;
using MyPlayer.viewmodels;

namespace MyPlayer.views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SaveState();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Waveform_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            if (sender is not FrameworkElement element) return;

            Point position = e.GetPosition(element);
            double percent = (position.X / element.ActualWidth) * 100;
            
            vm.SeekToPercent(percent);
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            if (vm.SelectedMusica == null) return;

            vm.PlayMusic(vm.SelectedMusica.Tag);
        }

        private void ListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lstMusicas.SelectedItem == null) return;
            lstMusicas.ScrollIntoView(lstMusicas.SelectedItem);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            if (e.Key == Key.F3)
            {
                txtFilter.Focus();
                txtFilter.SelectAll();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                if (!string.IsNullOrEmpty(vm.FilterText))
                {
                    vm.FilterText = string.Empty;
                    e.Handled = true;
                }
                return;
            }
        }
    }
}
