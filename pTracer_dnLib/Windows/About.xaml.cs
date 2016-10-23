using System;
using System.Windows;
using System.Windows.Input;


namespace pTracer_dn.Windows
{
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (MainWindow.Instance.AboutWindow != null) MainWindow.Instance.AboutWindow = null;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            this.Close();
        }

        private void RectHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //this.Close();
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
