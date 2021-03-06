using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace testdummy
{
    public partial class MainWindow : Window
    {
	Windows.About AboutWindow;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Window_Closed");
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (AboutWindow == null) AboutWindow = new Windows.About();
            AboutWindow.Owner = Application.Current.MainWindow;
            AboutWindow.Show();
        }
    }
}
