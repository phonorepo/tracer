using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace pTracer_dn.Windows
{
    public partial class askPID : Window
    {
        public askPID()
        {
            InitializeComponent();
        }

        private int _value = -1;

        public int Value
        {
            get { return _value; }
        }

        private void ForceNumber(object sender, TextCompositionEventArgs e)
        {
            Regex r = new Regex("[^0-9]+");
            e.Handled = r.IsMatch(e.Text);
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
            try
            {
                var tag = ((Button)sender).Tag;

                //if (tag.ToString() == ((int)DialogValues.OK).ToString())
                if (tag.ToString() == "5")
                {
                    _value = Int32.Parse(TextBox.Text);
                }
                else
                {
                    _value = -1;
                }
            }
            catch(Exception ex)
            {

            }
            this.Close();
        }

    }
}
