using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static pTracer_dn.Tracers;


namespace pTracer_dn
{
    // **********************************************************
    // ************ class MainWindow ****************************
    // **********************************************************
    public partial class MainWindow : Window
    {
        public bool Debugging = false;

        private static MainWindow instance;
        public static MainWindow Instance { get { return instance; } }

        public static string NewLine = Environment.NewLine;
        public string AppPath
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }
        public string LogPath
        {
            get { return System.IO.Path.Combine(MainWindow.Instance.AppPath, "Logs"); }
        }



        public string AppFullPath { get { return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); } }

        public string InjectedFile = string.Empty;


        public Analyzer _analyzer { get; set; }
        public Injector _injector { get; set; }
        public TraceSpy TSpy { get; set; }


        public bool AutoScroll = true;


        /// Windows
        public Windows.About AboutWindow;
        public Windows.Help HelpWindow;
        public Windows.Links LinksWindow;
        public Windows.askPID AskPidWindow;



        // Filter for Trace
        private int filterPID = -1;
        public int FilterPID
        {
            get { return filterPID; }
            set { filterPID = value; }
        }

        private string filterName = String.Empty;
        public string FilterName
        {
            get { return filterName; }
            set { filterName = value; }
        }

        private List<string> modDefs;
        private List<string> typDefs;
        private List<string> metDefs;

        private List<string> ModDefs
        {
            get { return modDefs; }
            set { modDefs = value; }
        }
        private List<string> TypDefs
        {
            get { return typDefs; }
            set { typDefs = value; }
        }

        private List<string> MetDefs
        {
            get { return metDefs; }
            set { metDefs = value; }
        }




        // **********************************************************
        // ************ MainWindow **********************************
        // **********************************************************

        public MainWindow()
        {
            InitializeComponent();
            instance = this;

            // check prerequisites
            Tools.preCheck();

            // add select all key binding to MainWindow
            if (lstBoxAnalyze != null)
            {
                InputBindings.Add(new KeyBinding(ApplicationCommands.SelectAll,
                              new KeyGesture(Key.A, ModifierKeys.Control)));
                CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, (_sender, _e) =>
                {
                    SelectAllMethodDefinitions();
                }));
            }


        }

        // Lazy hack to make it more visible if a togglebutton is checked
        // needs a nice style for it
        private void validateFilterButtons()
        {
            if (_analyzer != null)
            {
                if (_analyzer.FilterAssemblyName)
                {
                    //setToggleButtonChecked(toggleAssemblyName);
                }
                else
                {
                    //setToggleButtonUnChecked(toggleAssemblyNames);
                }

                if (_analyzer.FilterModules)
                {
                    setToggleButtonChecked(toggleModules);
                }
                else
                {
                    setToggleButtonUnChecked(toggleModules);
                }

                if (_analyzer.FilterReferences)
                {
                    setToggleButtonChecked(toggleReferences);
                }
                else
                {
                    setToggleButtonUnChecked(toggleReferences);
                }


                if (_analyzer.FilterTypes)
                {
                    setToggleButtonChecked(toggleTypes);
                }
                else
                {
                    setToggleButtonUnChecked(toggleTypes);
                }

                if (_analyzer.FilterMethods)
                {
                    setToggleButtonChecked(toggleMethods);
                }
                else
                {
                    setToggleButtonUnChecked(toggleMethods);
                }


                if (_analyzer.FilterParameters)
                {
                    setToggleButtonChecked(toggleParameters);
                }
                else
                {
                    setToggleButtonUnChecked(toggleParameters);
                }

                if (_analyzer.FilterVariables)
                {
                    setToggleButtonChecked(toggleVariables);
                }
                else
                {
                    setToggleButtonUnChecked(toggleVariables);
                }
            }
        }

        private void setToggleButtonChecked(System.Windows.Controls.Primitives.ToggleButton tButton)
        {
            if (tButton != null)
            {
                tButton.IsChecked = true;
                tButton.Padding = new Thickness(4, 4, 4, 4);
            }
        }
        private void setToggleButtonUnChecked(System.Windows.Controls.Primitives.ToggleButton tButton)
        {
            if (tButton != null)
            {
                tButton.IsChecked = false;
                tButton.Padding = new Thickness(1, 1, 1, 1);
            }
        }


        // a bit slow if many items
        public void SelectAllMethodDefinitions()
        {
            if (lstBoxAnalyze.SelectedItems.Count > 0)
            {
                lstBoxAnalyze.UnselectAll();
            }
            else
            {
                int i = 0;
                while (i < lstBoxAnalyze.Items.Count)
                {
                    ListBoxItem lbi = (ListBoxItem)lstBoxAnalyze.ItemContainerGenerator.ContainerFromIndex(i);
                    aPart a = lstBoxAnalyze.Items[i] as aPart;
                    if (a.Editible) lstBoxAnalyze.SelectedItems.Add(lstBoxAnalyze.Items.GetItemAt(i));
                    i++;
                }
            }
        }


        // **********************************************************
        // ************ Handle unhandled Exceptions *****************
        // **********************************************************

        public static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleUnhandledException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject != null)
            {
                Exception ex = (Exception)e.ExceptionObject;
                HandleUnhandledException(ex);
            }
        }

        private static void HandleUnhandledException(Exception ex)
        {
            string message = String.Format("Exception: {0}: {1} Source: {2} {3}", ex.GetType(), ex.Message, ex.Source, ex.StackTrace);
            //MessageBox.Show(message, "Error");
            MainWindow.Instance.mBox("Error", message);
            //Application.Current.Shutdown();
        }

        void AppDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            //\#if DEBUG   // In debug mode do not custom-handle the exception, let Visual Studio handle it
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // You are debugging
                e.Handled = false;
            }
            else
            {
                ShowUnhandeledException(e);
            }
        }

        void ShowUnhandeledException(System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            string errorMessage = string.Format("An application error occurred.\nPlease check whether your data is correct and repeat the action. If this error occurs again there seems to be a more serious malfunction in the application, and you better close it.\n\nError:{0}\n\nDo you want to continue?\n(if you click Yes you will continue with your work, if you click No the application will close)",

            e.Exception.Message + (e.Exception.InnerException != null ? "\n" +
            e.Exception.InnerException.Message : null));

            if (MessageBox.Show(errorMessage, "Application Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error) == MessageBoxResult.No)
            {
                if (MessageBox.Show("WARNING: The application will close. Any changes will not be saved!\nDo you really want to close it?", "Close the application!", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
            }
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("MyHandler caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
        }



        // **********************************************************
        // ************ Handle App Shutdown *************************
        // **********************************************************
        public static void ExitApp()
        {
            Tools.postCheck();
            if (instance.TSpy != null) instance.TSpy.Stop = true;
            Application.Current.Shutdown();
        }
        protected override void OnClosed(EventArgs e)
        {
            ExitApp();
            base.OnClosed(e);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ExitApp();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Tools.postCheck();
        }



        // **********************************************************
        // ************ Menu Clicks *********************************
        // **********************************************************

        private void menuFile_1_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog();
        }
        private void menuFolders_1_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(AppFullPath);
        }

        private void menuFolders_2_Click(object sender, RoutedEventArgs e)
        {
            Process eProc = new Process();
            eProc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            eProc.StartInfo.FileName = LogPath;
            eProc.Start();
        }
        private void menuFolders_3_Click(object sender, RoutedEventArgs e)
        {
            OpenSendToFolder();
        }

        private void menuFile_2_Click(object sender, RoutedEventArgs e)
        {
            ExitApp();
        }



        // **********************************************************
        // ************ Functions ***********************************
        // **********************************************************

        public void mBox(string Title, string Message)
        {
            Application.Current.Dispatcher.Invoke(
           System.Windows.Threading.DispatcherPriority.Normal,
           new Action(() =>
           {
               Windows.simpleDialog s = new Windows.simpleDialog();
               s.Owner = Application.Current.MainWindow;
               s.Title = Title;
               s.TextBox.Text = Message;
               s.Cancel.Visibility = Visibility.Collapsed;
               s.Yes.Visibility = Visibility.Collapsed;
               s.No.Visibility = Visibility.Collapsed;

               s.OK.IsDefault = true;

               s.ShowDialog();
           }));

        }

        public void UpdateFilter()
        {
            if (_analyzer != null)
            {
                _analyzer.UpdateFilter();
            }

        }

        private void OpenFileDialog()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            // open current application folder as default
            openFileDialog.InitialDirectory = AppFullPath;

            // Set filter
            //openFileDialog.DefaultExt = ".exe|.dll";
            openFileDialog.Filter = "(*.exe, *.dll)|*.exe;*.dll|(*.*)|*.*";

            // show OpenFileDialog
            Nullable<bool> result = openFileDialog.ShowDialog();

            // If OK get file name and update TextBox
            if (result == true)
            {
                string fileName = openFileDialog.FileName;
                txtBoxTargetFile.Text = fileName;
            }
        }

        public void OpenSendToFolder()
        {
            Process.Start(@"shell:sendto");
        }

        public void ScrollDown(ListView listview)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (AutoScroll && listview != null && listview.Items != null && listview.Items.Count > 0)
                {
                    int pos = listview.Items.Count - 1;
                    listview.ScrollIntoView(listview.Items[pos]);
                }
            }
            ));

        }

        public void ListViewCollectionChanged(Object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ScrollDown(listview);
        }

        public void StartTrace()
        {
            if (TSpy == null)
            {
                TSpy = new TraceSpy(listview);
            }

            // if filter is set
            if (FilterPID >= 0)
            {
                //MessageBox.Show("Set FilterPID: " + FilterPID);
                TSpy.FilterPID = FilterPID;
            }
            else
            {
                //MessageBox.Show("FilterPID < 0: " + FilterPID);
            }

            listview.ItemsSource = TSpy.Queue;

            ((System.Collections.Specialized.INotifyCollectionChanged)listview.ItemsSource).CollectionChanged -=
                new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ListViewCollectionChanged);

            ((System.Collections.Specialized.INotifyCollectionChanged)listview.ItemsSource).CollectionChanged +=
                new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ListViewCollectionChanged);

            //TSpy.Start();
            TSpy.StartReaderTask();
        }



        // **********************************************************
        // ************ Button Clicks *******************************
        // **********************************************************

        async void setInjectorResult(string Target)
        {
            string result = await _injector.InjectorTask;
            //txtBoxMessages.Text = result;
            InjectedFile = _injector.InjectedFile;
            if (File.Exists(InjectedFile)) txtBoxTargetFile.Text = InjectedFile;
            if (result != _injector.InjectedFile)
            {
                txtBoxMessages.Text = result;
                //MessageBox.Show("Injector Message: " + result);
                mBox("Injector Message", result);
            }
            else
            {
                txtBoxMessages.Text = DateTime.Now + " Injected in: " + InjectedFile + NewLine + "Orig: " + Target;
            }
        }
        private void btnInject_Click(object sender, RoutedEventArgs e)
        {
            if (txtBoxTargetFile != null && !String.IsNullOrEmpty(txtBoxTargetFile.Text))
            {
                if (lstBoxAnalyze != null && lstBoxAnalyze.SelectedItems.Count > 0)
                {
                    List<string> MetDefTokens = new List<string>();
                    string error = String.Empty;
                    foreach (aPart a in lstBoxAnalyze.SelectedItems)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(a.Token)) MetDefTokens.Add(a.Token);
                        }
                        catch (Exception ex)
                        {
                            error += ex.ToString() + NewLine;
                        }
                    }

                    //if (!String.IsNullOrEmpty(error)) MessageBox.Show(error);
                    if (!String.IsNullOrEmpty(error)) mBox("Injector Error", error);

                    if (File.Exists(txtBoxTargetFile.Text))
                    {
                        string TargetPath = txtBoxTargetFile.Text;
                        _injector = new Injector();

                        txtBoxMessages.Text = DateTime.Now + " Injector started ...";
                        _injector.StartInjectorTask(MetDefTokens, TargetPath, AppPath, true);

                        setInjectorResult(TargetPath);
                    }
                    else if (File.Exists(System.IO.Path.Combine(AppPath, txtBoxTargetFile.Text)))
                    {
                        string TargetPath = System.IO.Path.Combine(AppPath, txtBoxTargetFile.Text);
                        txtBoxTargetFile.Text = TargetPath;

                        txtBoxMessages.Text = DateTime.Now + " Injector started ...";
                        _injector.StartInjectorTask(MetDefTokens, TargetPath, AppPath, true);

                        setInjectorResult(TargetPath);
                    }
                    else
                    {
                        //MessageBox.Show("File not found: " + txtBoxTargetFile.Text);
                        mBox("Injector Error", "File not found: " + txtBoxTargetFile.Text);
                    }
                }
                else
                {
                    //MessageBox.Show("Select atleast one MethodDefinition to inject code.");
                    mBox("Injector Warning", "Select atleast one MethodDefinition to inject code.");
                }
            }
        }

        async void setAnalyzerResult()
        {
            string result = await _analyzer.AnalyzerTask;
            txtBoxMessages.Text = result;
        }

        private void btnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            if (txtBoxTargetFile != null && !String.IsNullOrEmpty(txtBoxTargetFile.Text))
            {
                string TargetPath = System.IO.Path.Combine(AppPath, txtBoxTargetFile.Text);
                if (File.Exists(TargetPath))
                {
                    _analyzer = new Analyzer(TargetPath);
                    validateFilterButtons();
                    txtBoxAnalyzer.DataContext = _analyzer;
                    lstBoxAnalyze.DataContext = _analyzer;

                    txtBoxMessages.Text = DateTime.Now + " Analyzer started ...";
                    string result = _analyzer.StartAnalyzerTask();
                    setAnalyzerResult();
                }
                else
                {
                    Debug.WriteLine(DateTime.Now + " File not found: " + TargetPath);
                }
            }
        }


        private void toggleMethods_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.ToggleButton tButton = sender as System.Windows.Controls.Primitives.ToggleButton;

            if (_analyzer != null)
            {
                if (tButton.IsChecked ?? false)
                {
                    _analyzer.FilterMethods = true;
                    validateFilterButtons();
                    UpdateFilter();
                }
                else
                {
                    _analyzer.FilterMethods = false;
                    validateFilterButtons();
                    UpdateFilter();
                }
            }
        }

        private void toggleTypes_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.ToggleButton tButton = sender as System.Windows.Controls.Primitives.ToggleButton;

            if (_analyzer != null)
            {
                if (tButton.IsChecked ?? false)
                {
                    _analyzer.FilterTypes = true;
                    validateFilterButtons();
                    UpdateFilter();
                }
                else
                {
                    _analyzer.FilterTypes = false;
                    validateFilterButtons();
                    UpdateFilter();
                }
            }
        }

        private void toggleModules_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.ToggleButton tButton = sender as System.Windows.Controls.Primitives.ToggleButton;

            if (_analyzer != null)
            {
                if (tButton.IsChecked ?? false)
                {
                    _analyzer.FilterModules = true;
                    validateFilterButtons();
                    UpdateFilter();
                }
                else
                {
                    _analyzer.FilterModules = false;
                    validateFilterButtons();
                    UpdateFilter();
                }
            }
        }

        private void toggleReferences_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.ToggleButton tButton = sender as System.Windows.Controls.Primitives.ToggleButton;

            if (_analyzer != null)
            {
                if (tButton.IsChecked ?? false)
                {
                    _analyzer.FilterReferences = true;
                    validateFilterButtons();
                    UpdateFilter();
                }
                else
                {
                    _analyzer.FilterReferences = false;
                    validateFilterButtons();
                    UpdateFilter();
                }
            }
        }

        private void toggleParameters_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.ToggleButton tButton = sender as System.Windows.Controls.Primitives.ToggleButton;

            if (_analyzer != null)
            {
                if (tButton.IsChecked ?? false)
                {
                    _analyzer.FilterParameters = true;
                    validateFilterButtons();
                    UpdateFilter();
                }
                else
                {
                    _analyzer.FilterParameters = false;
                    validateFilterButtons();
                    UpdateFilter();
                }
            }
        }

        private void toggleVariables_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.ToggleButton tButton = sender as System.Windows.Controls.Primitives.ToggleButton;

            if (tButton.IsChecked ?? false)
            {
                if (_analyzer != null)
                {
                    _analyzer.FilterVariables = true;
                    validateFilterButtons();
                    UpdateFilter();
                }
            }
            else
            {
                if (_analyzer != null)
                {
                    _analyzer.FilterVariables = false;
                    validateFilterButtons();
                    UpdateFilter();
                }
            }
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog();
        }

        private void btnSelectAllMethodDefinitions_Click(object sender, RoutedEventArgs e)
        {
            SelectAllMethodDefinitions();
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            string targetFile = string.Empty;

            if (!string.IsNullOrEmpty(txtBoxTargetFile.Text) && File.Exists(txtBoxTargetFile.Text))
            {
                targetFile = txtBoxTargetFile.Text;
            }
            else if (!string.IsNullOrEmpty(InjectedFile) && File.Exists(InjectedFile))
            {
                targetFile = InjectedFile;
            }

            if (!string.IsNullOrEmpty(targetFile))
            {
                Process p = Process.Start(targetFile);
                FilterPID = p.Id;

                // show TracePanel
                if (ExpanderTrace.IsExpanded != true) ExpanderTrace.IsExpanded = true;

                // set TraceButton as checked to be able to stop trace with this button
                if (btnTrace.IsChecked == false)
                {
                    btnTrace.IsChecked = true;

                    btnTrace.BorderThickness = new Thickness(4, 4, 4, 4);
                    btnTrace.Padding = new Thickness(4, 4, 4, 4);
                    btnTraceLabel2.Content = "Stop";
                }

                // Update TraceFilterPID-Button
                if (toggleTracePID.IsChecked == false)
                {
                    toggleTracePID.IsChecked = true;

                    toggleTracePID.BorderThickness = new Thickness(4, 4, 4, 4);
                    toggleTracePID.Padding = new Thickness(4, 4, 4, 4);
                    toggleTracePID.Content = "PID: " + FilterPID;
                }

                StartTrace();

            }
            else
            {
                //MessageBox.Show("targetFile is null or empty: " + targetFile);
            }
        }




        private void btnTrace_Click(object sender, RoutedEventArgs e)
        {

            System.Windows.Controls.Primitives.ToggleButton tButton = sender as System.Windows.Controls.Primitives.ToggleButton;

            if (tButton.IsChecked ?? false)
            {
                tButton.BorderThickness = new Thickness(4, 4, 4, 4);
                tButton.Padding = new Thickness(4, 4, 4, 4);
                btnTraceLabel2.Content = "Stop";

                StartTrace();
            }
            else
            {
                tButton.BorderThickness = new Thickness(1, 1, 1, 1);
                tButton.Padding = new Thickness(0, 0, 0, 0);
                btnTraceLabel2.Content = "Start";

                if (TSpy == null)
                {
                    TSpy = new TraceSpy(listview);
                }

                TSpy.Enabled = false;
            }
        }


        private void CopyToClipboardListView()
        {
            if (listview != null)
            {
                if (listview.Items.Count > 0)
                {
                    var sb = new StringBuilder();

                    if (listview.Items[0].GetType().ToString() == typeof(TraceLine).ToString())
                    {

                        List<TraceLine> selected = new List<TraceLine>();

                        foreach (var l in listview.Items)
                            selected.Add(l as TraceLine);

                        foreach (TraceLine l in selected)
                            sb.AppendLine(l.ToString());

                        try
                        {
                            //selected.OrderByDescending();
                            System.Windows.Clipboard.SetData(DataFormats.Text, sb.ToString());
                        }
                        catch (System.Runtime.InteropServices.COMException ce)
                        {
                            //MessageBox.Show("Unable to copy to the clipboard. Try again. " + NewLine + ce.ToString());
                            mBox("Copy Error", "Unable to copy to the clipboard. Try again. " + NewLine + ce.ToString());
                        }
                    }
                }
            }
        }



        private void toggleTracePID_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.ToggleButton tButton = sender as System.Windows.Controls.Primitives.ToggleButton;

            if (tButton.IsChecked ?? false)
            {
                tButton.BorderThickness = new Thickness(4, 4, 4, 4);
                tButton.Padding = new Thickness(4, 4, 4, 4);
                if (FilterPID >= 0) toggleTracePID.Content = "PID: " + FilterPID;
                else
                {
                    // ToDo: Ask for PID
                    Console.WriteLine("Ask for PID");
                    int pid = askPID();
                    Console.WriteLine("Ask for PID Result: " + pid.ToString());
                    if (pid >= 0)
                    {
                        Console.WriteLine("Ask for PID Result >= 0");
                        toggleTracePID.Content = "PID: " + pid.ToString();
                    }
                    else
                    {
                        Console.WriteLine("Ask for PID Result < 0");
                        toggleTracePID.Content = "PID: *";
                    }
                }
            }
            else
            {
                tButton.BorderThickness = new Thickness(1, 1, 1, 1);
                tButton.Padding = new Thickness(0, 0, 0, 0);
                toggleTracePID.Content = "PID: *";
                FilterPID = -1;
                if (TSpy != null)
                {
                    TSpy.FilterPID = FilterPID;
                }
            }
        }





        private void btnCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboardListView();
        }



        private void btnFUSION_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.ToggleButton tButton = sender as System.Windows.Controls.Primitives.ToggleButton;

            if (tButton.IsChecked ?? false)
            {
                Tools.FusionLogReg(false);
                tButton.BorderThickness = new Thickness(4, 4, 4, 4);
                tButton.Padding = new Thickness(4, 4, 4, 4);
                btnFUSIONLabel2.Content = "Stop";
            }
            else
            {
                Tools.FusionLogReg(true);
                tButton.BorderThickness = new Thickness(1, 1, 1, 1);
                tButton.Padding = new Thickness(0, 0, 0, 0);
                btnFUSIONLabel2.Content = "Start";
            }
        }


        // **********************************************************
        // ************ Windows *************************************
        // **********************************************************

        private void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            if (AboutWindow == null) AboutWindow = new Windows.About();
            AboutWindow.Owner = Application.Current.MainWindow;
            AboutWindow.Show();
        }

        private void menuHelp_Click(object sender, RoutedEventArgs e)
        {
            if (HelpWindow == null) HelpWindow = new Windows.Help();
            HelpWindow.Owner = Application.Current.MainWindow;
            HelpWindow.Show();
        }

        private void menuLinks_Click(object sender, RoutedEventArgs e)
        {
            if (LinksWindow == null) LinksWindow = new Windows.Links();

            LinksWindow.Owner = Application.Current.MainWindow;
            LinksWindow.Show();
        }

        private int askPID()
        {
            AskPidWindow = new Windows.askPID();
            AskPidWindow.Owner = Application.Current.MainWindow;
            AskPidWindow.ShowDialog();
            return AskPidWindow.Value;
        }

        public void testAAA()
        {
            Logging.PrintObj p = new Logging.PrintObj();

        }

        private void txtBoxFilterName_TextChanged(object sender, TextChangedEventArgs e)
        {
            filterName = ((TextBox)sender).Text;
            UpdateFilter();
            Trace.WriteLine("Filter updated. filter: " + filterName);
        }

    }
}


    

