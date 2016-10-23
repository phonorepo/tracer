using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace pTracer_dn
{
    public static class TraceListeners
    {
        public static void RemoveAllTraceListeners()
        {
            Trace.Listeners.Clear();
            /*
            while (Trace.Listeners.Count > 0)
            {
                Trace.Listeners.RemoveAt(0);
            }
            */
        }

        public static void AddTextWriterTL()
        {
            //RemoveAllTraceListeners();
            Trace.AutoFlush = true;
            System.Diagnostics.TextWriterTraceListener tListener = new System.Diagnostics.TextWriterTraceListener("TWTrace.txt");
            System.Diagnostics.Trace.Listeners.Add(tListener);
        }

        public static void AddDelimitedListTL()
        {
            //RemoveAllTraceListeners();
            Trace.AutoFlush = true;
            System.Diagnostics.DelimitedListTraceListener tListener = new System.Diagnostics.DelimitedListTraceListener("DLTrace.txt");
            System.Diagnostics.Trace.Listeners.Add(tListener);
        }

        public static void AddTextBoxTL(TextBox TargetTextbox)
        {
            if (TargetTextbox != null)
            {
                //RemoveAllTraceListeners();
                Trace.AutoFlush = true;
                TextBoxTraceListener tListener = new TextBoxTraceListener(TargetTextbox);
                System.Diagnostics.Trace.Listeners.Add(tListener);
            }
            else
            {
                MessageBox.Show("ERROR AddTextBoxTL: TextBox is null");
            }
        }


        public class TextBoxTraceListener : TraceListener
        {
            private TextBox targetTextbox;
            private StringSendDelegate invokeWrite;

            public TextBoxTraceListener(TextBox TargetTextbox)
            {
                targetTextbox = TargetTextbox;
                invokeWrite = new StringSendDelegate(SendString);
            }

            public override void Write(string message)
            {
                targetTextbox.Dispatcher.Invoke(invokeWrite, new object[] { message });
            }

            public override void WriteLine(string message)
            {
                targetTextbox.Dispatcher.Invoke(invokeWrite, new object[]
                    { message + Environment.NewLine });
            }

            private delegate void StringSendDelegate(string message);
            private void SendString(string message)
            {
                targetTextbox.Text += message;
            }
        }


    }
}
