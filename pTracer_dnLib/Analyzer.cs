using dnlib.DotNet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace pTracer_dn
{
    #region class aPart
    public class aPart : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string token;
        public string Token
        {
            get { return token; }
            set
            {
                token = value;
                OnPropertyChanged("Token");
            }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        private string virtualname;
        public string VirtualName
        {
            get { return virtualname; }
            set
            {
                virtualname = value;
                OnPropertyChanged("VirtualName");
            }
        }

        private bool editible;
        public bool Editible
        {
            get { return editible; }
            set
            {
                editible = value;
                OnPropertyChanged("Editible");
            }
        }

        private Types type;
        public Types Type
        {
            get { return type; }
            set
            {
                type = value;
                OnPropertyChanged("Type");
            }
        }

        public Image Image { get; set; }

        public enum Types
        {
            AssemblyName,
            ModuleDefinition,
            TypeDefinition,
            MethodDefinition,
            ParameterDefinition,
            VariableDefinition,
            AssemblyReference
        };



        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
    #endregion

    #region class Analyzer
    public class Analyzer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static string NewLine = Environment.NewLine;

        private object _analyzerLock = new object();
        public CancellationTokenSource cancelTS = new CancellationTokenSource();
        public System.Threading.Tasks.Task<string> AnalyzerTask;

        private string targetPath;
        public string TargetPath
        {
            get { return targetPath; }
            set { targetPath = value; }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        void BindingOperations_CollectionRegistering(object sender, CollectionRegisteringEventArgs e)
        {
            Debug.WriteLine("CollectionRegistering Event");
            if (e.Collection == aParts)
            {
                Debug.WriteLine("CollectionRegistering Event: e.Collection == Queue");
                BindingOperations.EnableCollectionSynchronization(aParts, _analyzerLock);
            }
        }

        private bool filterAssemblyName = true;
        private bool filterModules = false;
        private bool filterReferences = true;
        private bool filterTypes = false;
        private bool filterMethods = false;
        private bool filterParameters = true;
        private bool filterVariables = true;
	private bool filterModuleNames = true;

        public bool FilterAssemblyName
        {
            get { return filterAssemblyName; }
            set { filterAssemblyName = value; }
        }
        public bool FilterModules
        {
            get { return filterModules; }
            set { filterModules = value; }
        }

        public bool FilterReferences
        {
            get { return filterReferences; }
            set { filterReferences = value; }
        }

        public bool FilterTypes
        {
            get { return filterTypes; }
            set { filterTypes = value; }
        }
        public bool FilterMethods
        {
            get { return filterMethods; }
            set { filterMethods = value; }
        }
        public bool FilterParameters
        {
            get { return filterParameters; }
            set { filterParameters = value; }
        }
        public bool FilterVariables
        {
            get { return filterVariables; }
            set { filterVariables = value; }
        }

        public bool FilterModuleNames
        {
            get { return filterModuleNames; }
            set { filterModuleNames = value; }
        }

        public ObservableCollection<aPart> aParts { get; set; }

        private string textView;
        public string TextView
        {
            get { return textView; }
            set
            {
                textView = value;
                OnPropertyChanged("TextView");
            }
        }



        private AssemblyResolver assmeblyResolver;
        public AssemblyResolver AssmeblyResolver
        {
            get { return assmeblyResolver; }
            set
            {
                assmeblyResolver = value;
                OnPropertyChanged("AssmeblyResolver");
            }
        }

        public void AddSearchPath(string Path)
        {
            if (!string.IsNullOrEmpty(Path) && Directory.Exists(Path))
            {
                if (AssmeblyResolver == null) AssmeblyResolver = new AssemblyResolver();
                AssmeblyResolver.PostSearchPaths.Add(Path);
            }
        }

        private MyToolkit.Collections.ObservableCollectionView<aPart> view;
        public MyToolkit.Collections.ObservableCollectionView<aPart> View
        {
            get { return view; }
            set
            {
                view = value;
                OnPropertyChanged("View");
            }
        }

        private ObservableCollection<aPart> _aPartsFiltered;
        public ObservableCollection<aPart> aPartsFiltered
        {
            get
            {
                if (_aPartsFiltered != null && _aPartsFiltered.Count > 0)
                {
                    foreach (var a in _aPartsFiltered)
                    {
                        if (a.Type == aPart.Types.MethodDefinition && !FilterMethods) _aPartsFiltered.Add(a);
                        else if (a.Type == aPart.Types.AssemblyReference && !FilterReferences) _aPartsFiltered.Add(a);
                        else if (a.Type == aPart.Types.ParameterDefinition && !FilterParameters) _aPartsFiltered.Add(a);
                        else if (a.Type == aPart.Types.VariableDefinition && !FilterVariables) _aPartsFiltered.Add(a);

                        //else if (a.Type == aPart.Types.ModuleDefinition && !FilterModules) _aPartsFiltered.Add(a);
                        else if (a.Type == aPart.Types.ModuleDefinition)
                        {
                            Trace.WriteLine("a.Type = ModDef - Name: " + a.Name);
                            if (!FilterModuleNames)
                            {
                                if (!FilterMethods && a.Type == aPart.Types.ModuleDefinition)
                                {
                                    Trace.WriteLine("!FilterModuleNames  && a.Type == aPart.Types.ModuleDefinition -->  Add(a);");
                                    _aPartsFiltered.Add(a);
                                }
                            }
                            else if(a.Type == aPart.Types.ModuleDefinition && a.Name.StartsWith(MainWindow.Instance.FilterName))
                            {
                                Trace.WriteLine("!FilterModuleNames  && && a.Name.StartsWith(MainWindow.Instance.FilterName) -->  Add(a);");
                                _aPartsFiltered.Add(a);
                            }
                        }
                        else _aPartsFiltered.Add(a);
                    }
                }
                return _aPartsFiltered;
            }
            set { _aPartsFiltered = value; }
        }

        public Analyzer(string TargetPath)
        {
            BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;
            targetPath = TargetPath;

            View = new MyToolkit.Collections.ObservableCollectionView<aPart>();
            aParts = new System.Collections.ObjectModel.ObservableCollection<aPart>();
        }


        public void UpdateFilter()
        {
            // initial "false" condition just to start "OR" clause with
            var predicate = PredicateBuilder.False<aPart>();

            if (!FilterAssemblyName) predicate = predicate.Or(p => p.Type == aPart.Types.AssemblyName);
            if (!FilterModules) predicate = predicate.Or(p => p.Type == aPart.Types.ModuleDefinition);
            if (!FilterReferences) predicate = predicate.Or(p => p.Type == aPart.Types.AssemblyReference);
            if (!FilterTypes) predicate = predicate.Or(p => p.Type == aPart.Types.TypeDefinition);
            
            if (!FilterParameters) predicate = predicate.Or(p => p.Type == aPart.Types.ParameterDefinition);
            if (!FilterVariables) predicate = predicate.Or(p => p.Type == aPart.Types.VariableDefinition);

            if (!FilterModuleNames)
            {
                if (!FilterMethods)
                {
                    predicate = predicate.Or(p => p.Type == aPart.Types.MethodDefinition);
                }
            }
            else
            {
                if (!FilterMethods)
                {
                    predicate = predicate.Or(p => p.Type == aPart.Types.MethodDefinition && p.Name.StartsWith(MainWindow.Instance.FilterName));
                }
            }

            Application.Current.Dispatcher.Invoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {
                view.Filter = predicate.Compile();
            }));
        }

        public void ForceStop()
        {
            if (AnalyzerTask != null)
            {
                cancelTS.Cancel();
            }

        }

        public string StartAnalyzerTask()
        {
            string ReturnString = String.Empty;

            AnalyzerTask = System.Threading.Tasks.Task.Factory.StartNew<string>(() =>
            {
                if (targetPath != null && File.Exists(targetPath))
                {
                    Debug.WriteLine("StartAnalyzerTask for: " + targetPath);
                    ReturnString = Analyze(targetPath);
                }
                return ReturnString;
            }, cancelTS.Token);

            return ReturnString;
        }

        public string Analyze(string assemblyPath, bool WithTrace = false)
        {
            // for debugging
            //return analyze(assemblyPath, WithTrace);

            try
            {
                return analyze(assemblyPath, WithTrace);
            }
            catch (Exception ex)
            {
                return "Analyze exception: " + ex.ToString();
            }
        }


        private void GenerateImage(string ResourceName)
        {
            Application.Current.Dispatcher.Invoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {
                _tempImage = newImage(ResourceName);
            }));
        }

        System.Windows.Controls.Image _tempImage;
        private System.Windows.Controls.Image newImage(string ResourceName)
        {
            System.Windows.Controls.Image newImage = new System.Windows.Controls.Image();
            System.Windows.Media.DrawingImage drawingImage = MainWindow.Instance.FindResource(ResourceName) as System.Windows.Media.DrawingImage;
            drawingImage.Freeze();
            newImage.Source = drawingImage;
            System.Windows.Controls.ToolTipService.SetToolTip(newImage, ResourceName);
            return newImage;
        }



        private string analyze(string assemblyPath, bool WithTrace = false)
        {
            string fileName = Path.GetFileName(assemblyPath);

            StringBuilder ReturnString = new StringBuilder();
            
            string s = String.Empty;

            // Load assembly
            AssemblyDef asmDef = AssemblyDef.Load(assemblyPath);
            ModuleDefMD mod = ModuleDefMD.Load(assemblyPath);
            // check references

            foreach (ModuleDef modDef in asmDef.Modules)
            {

                s = modDef.Name + "  (" + modDef.MDToken.Rid + ")" + NewLine;
                ReturnString.Append(s);
                if (WithTrace) Trace.WriteLine(s);

                aPart a = new aPart();
                a.Name = modDef.Name;
                a.VirtualName = s;
                a.Token = modDef.MDToken.ToString();
                a.Type = aPart.Types.ModuleDefinition;
                a.Editible = false;

                // Icon
                GenerateImage("MoImage");
                a.Image = _tempImage;

                aParts.Add(a);


                foreach (var assemRef in modDef.GetAssemblyRefs())
                {
                    s = assemRef.Name + "  (" + assemRef.MDToken.Rid + ")" + NewLine;
                    ReturnString.Append(s);
                    if (WithTrace) Trace.WriteLine(s);

                    a = new aPart();
                    a.Name = assemRef.Name;
                    a.VirtualName = s;
                    a.Token = assemRef.MDToken.ToString();
                    a.Type = aPart.Types.AssemblyReference;
                    a.Editible = false;

                    // Icon
                    GenerateImage("ReImage");
                    a.Image = _tempImage;

                    aParts.Add(a);

                }


                foreach (TypeDef typDef in modDef.Types)
                {
                    s = "    " + typDef.Name + "  (" + typDef.MDToken.Rid + ")" + NewLine;
                    ReturnString.Append(s);
                    if (WithTrace) Trace.WriteLine(s);

                    a = new aPart();
                    a.Name = typDef.Name;
                    a.VirtualName = s;
                    a.Token = typDef.MDToken.ToString();
                    a.Type = aPart.Types.TypeDefinition;
                    a.Editible = false;

                    // Icon
                    GenerateImage("TyImage");
                    a.Image = _tempImage;

                    aParts.Add(a);

                    foreach (MethodDef metDef in typDef.Methods)
                    {
                        s = "        " + metDef.Name + "  (" + metDef.MDToken.Rid + ")" + NewLine;
                        ReturnString.Append(s);
                        if (WithTrace) Trace.WriteLine(s);

                        a = new aPart();
                        a.Name = metDef.Name;
                        a.VirtualName = s;
                        a.Token = metDef.MDToken.ToString();
                        a.Type = aPart.Types.MethodDefinition;
                        a.Editible = true;

                        // Icon
                        GenerateImage("MeImage");
                        a.Image = _tempImage;

                        aParts.Add(a);

                        if (metDef.Parameters != null && metDef.Parameters.Count > 0)
                        {
                            foreach (Parameter param in metDef.Parameters)
                            {
                                //s = "            Type: " + param.ParamDef.ElementType.ToString() + " Name: " + param.Name + NewLine;
                                s = "             Name: " + param.Name + NewLine;
                                if(param.ParamDef != null) s += "            Attributes: " + param.ParamDef.Attributes.ToString();
                                ReturnString.Append(s);
                                if (WithTrace) Trace.WriteLine(s);

                                a = new aPart();
                                a.Name = param.Name;
                                a.VirtualName = s;
                                if (param.ParamDef != null) a.Token = param.ParamDef.MDToken.ToString();
                                a.Type = aPart.Types.ParameterDefinition;
                                a.Editible = false;

                                // Icon
                                GenerateImage("PaImage");
                                a.Image = _tempImage;

                                aParts.Add(a);
                            }
                        }

                        if (metDef.Body != null && metDef.Body.Variables != null && metDef.Body.Variables.Count > 0)
                        {
                            foreach (dnlib.DotNet.Emit.Local variable in metDef.Body.Variables)
                            {
                                s = "            Type: " + variable.Type.ToString() + " Name: " + variable.Name + NewLine;
                                s += "            Index: " + variable.Index.ToString();
                                ReturnString.Append(s);
                                if (WithTrace) Trace.WriteLine(s);

                                a = new aPart();
                                a.Name = variable.Name;
                                a.VirtualName = s;
                                a.Type = aPart.Types.VariableDefinition;
                                a.Editible = false;

                                // Icon
                                GenerateImage("PaImage");
                                a.Image = _tempImage;

                                aParts.Add(a);
                            }
                        }
                    }
                }
            }

            TextView = ReturnString.ToString();
            View = new MyToolkit.Collections.ObservableCollectionView<aPart>(aParts);
            UpdateFilter();

            return ReturnString.ToString();
        }

    }
    #endregion

}
