using System;
using System.Diagnostics;
using System.Windows;

namespace pTracer_dn.Windows
{
    public partial class Links : Window
    {
        public Links()
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
