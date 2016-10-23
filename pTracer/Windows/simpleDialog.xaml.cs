using System;
using System.Windows;
using System.Windows.Controls;


namespace pTracer.Windows
{
    public partial class simpleDialog : Window
    {
        public simpleDialog()
        {
            InitializeComponent();
        }

        private int _value = 0;
        private string _text = "?";

        public int Value
        {
            get { return _value; }
        }
        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }



        public enum DialogValues
        {
            No = 0,
            Yes = 1,
            NoToAll = 2,
            YesToAll = 3,
            Cancel = 4,
            OK = 5
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var tag = ((Button)sender).Tag;
            _value = Int32.Parse(tag.ToString());
            this.Close();
        }

        private void simpleDialog_Closed(object sender, EventArgs e)
        {
        }

    }
}
