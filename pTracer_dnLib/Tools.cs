using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;

namespace pTracer_dn
{
    public static class Tools
    {
        public static bool Debugging = MainWindow.Instance.Debugging;
        public static string regExePath = System.IO.Path.Combine(Environment.SystemDirectory,"reg.exe");

        public static string NewLine = Environment.NewLine;

        public static void preCheck()
        {
            // check Directories exist and if not create
            if (!System.IO.Directory.Exists(MainWindow.Instance.LogPath))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(MainWindow.Instance.LogPath);
                }
                catch (Exception ex)
                {
                    if (Debugging) Debug.WriteLine("preCheck: " + ex.ToString());
                }
            }

            // check if Fusion logging is already enabled
            if(isFusionLogEnabled())
            {
                MainWindow.Instance.btnFUSION.IsChecked = true;
                MainWindow.Instance.btnFUSIONLabel2.Content = "Stop";
            }
        }

        public static void postCheck()
        {
            //ask to disable Fusion logs because it needs a lot of resources

            if (isFusionLogEnabled())
            {
                Windows.simpleDialog s = new Windows.simpleDialog();
                s.Title = "Disable logging now?";
                s.TextBox.Text = "FUSION logging is still enabled, disable now?" + Environment.NewLine + "Logging is using a lot of resources! It is recommended to disable it when not needed anymore.";
                s.ShowDialog();
                if(s.Value == (int)Windows.simpleDialog.DialogValues.Yes)
                {
                    FusionLogReg(true);
                }
            }
        }

        public static bool isFusionLogEnabled()
        {
            bool ret = false;

            try
            {
                RegistryKey basekey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                RegistryKey key = basekey.OpenSubKey("Software\\Microsoft\\Fusion\\", false);

                string rEnableLog = RegObjectToString(key, "EnableLog");
                string rForceLog = RegObjectToString(key, "ForceLog");

                if (rEnableLog != null && rEnableLog == "1") ret = true;
                if (rForceLog != null && rForceLog == "1") ret = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("INFO - isFusionLogEnabled: " + ex.ToString());
            }
            return ret;
        }

        public static string RegObjectToString(RegistryKey k, string valueName)
        {
            object o = k.GetValue(valueName);

            switch (k.GetValueKind(valueName))
            {
                case RegistryValueKind.String:
                    return (string) o;
                case RegistryValueKind.ExpandString:
                    return (string)o;
                case RegistryValueKind.Binary:
                    string retString = String.Empty;
                    foreach (byte b in (byte[])o)
                    {
                         retString += String.Format("{0:x2} ", b);
                    }
                    return retString;
                case RegistryValueKind.DWord:
                    return Convert.ToString((Int32)o);
                case RegistryValueKind.QWord:
                    return Convert.ToString((Int64)o);
                case RegistryValueKind.MultiString:
                    string retMString = String.Empty;
                    foreach (string s in (string[])o)
                    {
                        retMString += String.Format("[{0:s}], ", s);
                    }
                    return retMString;
                default:
                    Console.WriteLine("Value = (Unknown)");
                    return String.Empty;
            }
        }


        public static Process runElevated(string ExePath, string Arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(ExePath, Arguments);
            startInfo.Verb = "runas";
            return System.Diagnostics.Process.Start(startInfo);
        }

        private static void restartElevated()
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = System.AppDomain.CurrentDomain.FriendlyName;

            try
            {
                Process.Start(proc);
            }
            catch
            {
                // User canceled elevation, return and exit
                return;
            }

            MainWindow.ExitApp();
        }


        public static void FusionLogReg(bool disable)
        {
            int Value = 0;
            if (disable == false) Value = 1;

            try
            {
                //RegistryKey basekey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, Environment.MachineName, RegistryView.Registry64);
                RegistryKey basekey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                RegistryKey key = basekey.OpenSubKey("Software\\Microsoft\\Fusion\\", true);
                //Object value = key.GetValue("EnableLog");
                key.SetValue("EnableLog", Value, RegistryValueKind.DWord);
                key.SetValue("ForceLog", Value, RegistryValueKind.DWord);
                key.SetValue("LogFailures", Value, RegistryValueKind.DWord);
                key.SetValue("LogResourceBinds", Value, RegistryValueKind.DWord);

                key.SetValue("LogPath", MainWindow.Instance.LogPath, RegistryValueKind.String);

                key.Close();
            }
            catch (System.UnauthorizedAccessException ex)
            {
                Windows.simpleDialog s = new Windows.simpleDialog();
                s.Title = "Restart elevated?";
                s.TextBox.Text = "Writing to the registry requires elevation. Restart now elevated?";
                s.ShowDialog();
                if (s.Value == (int)Windows.simpleDialog.DialogValues.Yes)
                {
                    restartElevated();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("FusionLogReg: " + ex.ToString());
            }

        }
    }
}
