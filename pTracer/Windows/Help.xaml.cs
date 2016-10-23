using System;
using System.Diagnostics;
using System.Windows;


namespace pTracer.Windows
{
    public partial class Help : Window
    {
        public Help()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (MainWindow.Instance.LinksWindow != null) MainWindow.Instance.LinksWindow = null;
        }

        private void FollowLink(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }

}
