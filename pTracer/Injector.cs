using Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace pTracer
{
    public class Injector : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }


        public System.Threading.Tasks.Task<string> InjectorTask;

        public string NewLine = Environment.NewLine;


        private DefaultAssemblyResolver assmeblyResolver;
        public DefaultAssemblyResolver AssmeblyResolver
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
                if (AssmeblyResolver == null) AssmeblyResolver = new DefaultAssemblyResolver();
                AssmeblyResolver.AddSearchDirectory(Path);
            }
        }


        private string injectedFile;
        public string InjectedFile
        {
            get { return injectedFile; }
            set
            {
                injectedFile = value;
                OnPropertyChanged("InjectedFile");
            }
        }

        private string text;
        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                OnPropertyChanged("Text");
            }
        }


        public string InjectDeepTrace(List<string> MethodToken, string assemblyPath, string outputDirectory, bool WithTrace = false)
        {
            return injectDeepTrace(MethodToken, assemblyPath, outputDirectory, WithTrace);
        }

        public void StartInjectorTask(List<string> MethodToken, string assemblyPath, string outputDirectory, bool WithTrace = false)
        {
            InjectorTask = System.Threading.Tasks.Task.Factory.StartNew<string>(() =>
            {
                return InjectDeepTrace(MethodToken, assemblyPath, outputDirectory, WithTrace);
            });

        }

        private string injectDeepTrace(List<string> MethodToken, string assemblyPath, string outputDirectory, bool WithTrace = false)
        {
            AssemblyDefinition asmDef;

            // New assembly path
            string fileName = Path.GetFileName(assemblyPath);

            // Append Date and Time to new filename
            string newPath = Path.Combine(outputDirectory, DateTime.UtcNow.ToString("yyyy-MM-dd HH.mm.ss.fff", CultureInfo.InvariantCulture) + "_" + fileName);

            // Check if Output directory already exists, if not, create one
            if (!Directory.Exists(outputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            try
            {
                // AssemblyResolver
                if (AssmeblyResolver == null) AssmeblyResolver = new DefaultAssemblyResolver();
                if (Directory.Exists(Path.GetDirectoryName(assemblyPath))) AddSearchPath(Path.GetDirectoryName(assemblyPath));

                // Load assembly
                asmDef = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { AssemblyResolver = AssmeblyResolver });


                foreach (ModuleDefinition modDef in asmDef.Modules)
                {
                    foreach (TypeDefinition typDef in modDef.Types)
                    {
                        foreach (MethodDefinition metDef in typDef.Methods)
                        {
                            if (MethodToken.Contains(metDef.MetadataToken.ToString()))
                            {
                                if(WithTrace) Trace.WriteLine("Found method " + metDef.ToString() + " Token: " + metDef.MetadataToken.ToString());

                                try
                                {
                                    string variablesInfo = string.Empty;
                                    if (metDef.Body != null && metDef.Body.Variables != null && metDef.Body.Variables.Count > 0)
                                    {
                                        foreach (var variable in metDef.Body.Variables)
                                        {
                                            string varInfo = "            Variable - Type: " + variable.VariableType.ToString() + " Name: " + variable.Name + NewLine;
                                            varInfo += "            Index: " + variable.Index.ToString();
                                            if (WithTrace) Trace.WriteLine(varInfo);
                                            variablesInfo += varInfo;
                                        }
                                    }

                                    if (metDef != null && metDef.Body != null)
                                    {
                                        // Get ILProcessor
                                        ILProcessor ilProcessor = metDef.Body.GetILProcessor();

                                        ///
                                        /// Simple TraceLine
                                        ///

                                        /*
                                        // Load fully qualified method name as string
                                        Instruction i1 = ilProcessor.Create(
                                        OpCodes.Ldstr,
                                        metDef.ToString() + variablesInfo
                                        );
                                        ilProcessor.InsertBefore(metDef.Body.Instructions[0], i1);

                                        // Call the method which would write tracing info
                                        Instruction i2 = ilProcessor.Create(
                                        OpCodes.Call,
                                        metDef.Module.Import(
                                        typeof(Trace).GetMethod("WriteLine", new[] { typeof(string) })
                                        )
                                        );
                                        ilProcessor.InsertAfter(i1, i2);

                                        */


                                        ///
                                        /// PrintObj (injected Logging.dll)
                                        /// extended by using code and comments from CInject
                                        /// https://codeinject.codeplex.com/
                                        ///
                                        Instruction firstExistingInstruction = metDef.Body.Instructions[0];

                                        // import our pritObj Class
                                        Type inputType = typeof(PrintObj);
                                        TypeReference _printObj = asmDef.MainModule.Import(inputType);
                                        MethodReference _printObjCtor = asmDef.MainModule.Import(inputType.GetConstructors().First(c => !c.IsStatic));


                                        Type t = typeof(System.Reflection.MethodBase);
                                        string methodname = "GetCurrentMethod";
                                        MethodReference _methodGetCurrentMethod = asmDef.MainModule.Import(t.GetMethod(methodname));

                                        methodname = "set_CurrentMethod";
                                        MethodReference _methodSetMethod = asmDef.MainModule.Import(inputType.GetMethod(methodname));


                                        t = typeof(System.Reflection.Assembly);
                                        methodname = "GetExecutingAssembly";
                                        MethodReference _methodGetExecutingAssembly = asmDef.MainModule.Import(t.GetMethod(methodname));

                                        methodname = "set_CurrentAssembly";
                                        MethodReference _methodSetExecutingAssembly = asmDef.MainModule.Import(inputType.GetMethod(methodname));


                                        methodname = "get_CurrentArguments";
                                        MethodReference _methodGetArguments = asmDef.MainModule.Import(inputType.GetMethod(methodname));

                                        methodname = "set_CurrentArguments";
                                        MethodReference _methodSetArguments = asmDef.MainModule.Import(inputType.GetMethod(methodname));

                                        methodname = "PrintArgs";
                                        MethodReference _methodPrintArgs = asmDef.MainModule.Import(inputType.GetMethod(methodname));



                                        // Add new variables
                                        metDef.Body.InitLocals = true;

                                        VariableDefinition printO = new VariableDefinition(_printObj);
                                        metDef.Body.Variables.Add(printO);

                                        var objTypeArr = asmDef.MainModule.Import(typeof(object[]));
                                        VariableDefinition oArray = new VariableDefinition(objTypeArr);
                                        metDef.Body.Variables.Add(oArray);


                                        // insert appended but always before the first existing original instruction

                                        // create constructor of CInjection
                                        Instruction i22 = ilProcessor.Create(OpCodes.Newobj, _printObjCtor);
                                        ilProcessor.InsertBefore(firstExistingInstruction, i22);
                                        Instruction i23 = ilProcessor.Create(OpCodes.Stloc_S, printO);
                                        ilProcessor.InsertBefore(firstExistingInstruction, i23);

                                        Instruction i6 = ilProcessor.Create(OpCodes.Ldloc_S, printO);
                                        ilProcessor.InsertBefore(firstExistingInstruction, i6);


                                        // create parameter of GetCurrentMethod
                                        Instruction i24 = ilProcessor.Create(OpCodes.Call, _methodGetCurrentMethod);
                                        ilProcessor.InsertBefore(firstExistingInstruction, i24);

                                        Instruction i25 = ilProcessor.Create(OpCodes.Callvirt, _methodSetMethod);
                                        ilProcessor.InsertBefore(firstExistingInstruction, i25);

                                        Instruction i26 = ilProcessor.Create(OpCodes.Nop);
                                        ilProcessor.InsertBefore(firstExistingInstruction, i26);


                                        Instruction i27 = ilProcessor.Create(OpCodes.Ldloc_S, printO);
                                        ilProcessor.InsertBefore(firstExistingInstruction, i27);


                                        Instruction i5 = ilProcessor.Create(OpCodes.Ldc_I4, metDef.Parameters.Count);
                                        ilProcessor.InsertBefore(firstExistingInstruction, i5);

                                        var objType = asmDef.MainModule.Import(typeof(object));
                                        Instruction i4 = ilProcessor.Create(OpCodes.Newarr, objType);
                                        ilProcessor.InsertBefore(firstExistingInstruction, i4);

                                        Instruction i3 = ilProcessor.Create(OpCodes.Stloc_S, oArray);
                                        ilProcessor.InsertBefore(firstExistingInstruction, i3);



                                        for (int i = 0; i < metDef.Parameters.Count; i++)
                                        {
                                            bool processAsNormal = true;

                                            if (metDef.Parameters[i].ParameterType.IsByReference)
                                            {
                                                //* Sample Instruction set:
                                                //* L_002a: ldloc.2 
                                                //* L_002b: ldc.i4.0 
                                                //* L_002c: ldarg.1 
                                                //* L_002d: ldind.ref 
                                                //* L_002e: stelem.ref 
                                                //* 

                                                Instruction i11 = ilProcessor.Create(OpCodes.Ldloc_S, oArray);
                                                Instruction i10 = ilProcessor.Create(OpCodes.Ldc_I4, i);
                                                Instruction i9 = ilProcessor.Create(OpCodes.Ldarg, metDef.Parameters[i]);
                                                Instruction i8 = ilProcessor.Create(OpCodes.Ldind_Ref);
                                                Instruction i7 = ilProcessor.Create(OpCodes.Stelem_Ref);

                                                ilProcessor.InsertBefore(firstExistingInstruction, i11);
                                                ilProcessor.InsertBefore(firstExistingInstruction, i10);
                                                ilProcessor.InsertBefore(firstExistingInstruction, i9);
                                                ilProcessor.InsertBefore(firstExistingInstruction, i8);
                                                ilProcessor.InsertBefore(firstExistingInstruction, i7);

                                                processAsNormal = false;
                                            }
                                            //else if (metDef.Parameters[i].ParameterType.IsArray)
                                            //{

                                            //}
                                            //else if (metDef.Parameters[i].ParameterType.IsDefinition) // delegate needs no seperate handling
                                            //{

                                            //}
                                            else if (metDef.Parameters[i].ParameterType.IsFunctionPointer)
                                            {

                                            }
                                            //else if (metDef.Parameters[i].ParameterType.IsOptionalModifier)
                                            //{

                                            //}
                                            else if (metDef.Parameters[i].ParameterType.IsPointer)
                                            {

                                            }
                                            else
                                            {
                                                processAsNormal = true;
                                            }

                                            if (processAsNormal)
                                            {
                                                /* Sample Instruction set: for simple PARAMETER
                                                 * L_0036: ldloc.s objArray
                                                 * L_0038: ldc.i4 0
                                                 * L_003d: ldarg array
                                                 * L_0041: box Int32    <-------------- anything can be here
                                                 * L_0046: stelem.ref 
                                                 * */

                                                /* Sample Instruction set: for ARRAY
                                                 * L_0036: ldloc.s objArray
                                                 * L_0038: ldc.i4 0
                                                 * L_003d: ldarg array
                                                 * L_0041: box string[]
                                                 * L_0046: stelem.ref 
                                                 * */

                                                Instruction i12 = ilProcessor.Create(OpCodes.Ldloc_S, oArray);
                                                Instruction i13 = ilProcessor.Create(OpCodes.Ldc_I4, i);
                                                Instruction i14 = ilProcessor.Create(OpCodes.Ldarg, metDef.Parameters[i]);
                                                Instruction i15 = ilProcessor.Create(OpCodes.Box, metDef.Parameters[i].ParameterType);
                                                Instruction i16 = ilProcessor.Create(OpCodes.Stelem_Ref);

                                                ilProcessor.InsertBefore(firstExistingInstruction, i12);
                                                ilProcessor.InsertBefore(firstExistingInstruction, i13);
                                                ilProcessor.InsertBefore(firstExistingInstruction, i14);
                                                ilProcessor.InsertBefore(firstExistingInstruction, i15);
                                                ilProcessor.InsertBefore(firstExistingInstruction, i16);
                                            }
                                        }

                                        Instruction i17 = ilProcessor.Create(OpCodes.Ldloc_S, oArray);
                                        Instruction i18 = ilProcessor.Create(OpCodes.Callvirt, _methodSetArguments);

                                        ilProcessor.InsertBefore(firstExistingInstruction, i17);
                                        ilProcessor.InsertBefore(firstExistingInstruction, i18);

                                        // call PrintArgs

                                        Instruction i19 = ilProcessor.Create(OpCodes.Ldloc_S, printO);
                                        Instruction i20 = ilProcessor.Create(OpCodes.Callvirt, _methodPrintArgs);

                                        ilProcessor.InsertBefore(firstExistingInstruction, i19);
                                        ilProcessor.InsertBefore(firstExistingInstruction, i20);

                                    }
                                    else
                                    {
                                        if (WithTrace) Trace.WriteLine("metDef or metDef.Body was null");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.ToString());
                                    MessageBox.Show(ex.ToString());
                                }

                            }
                        }

                    }
                }
                // Save modified assembly
                asmDef.Write(newPath, new WriterParameters() { WriteSymbols = true });
            }
            catch (Exception ex)
            {
                if (WithTrace) Trace.WriteLine(DateTime.Now + " injectDeepTrace exception: " + ex.ToString());

                return (DateTime.Now + " injectDeepTrace exception: " + ex.ToString());
            }

            InjectedFile = newPath;
            Text = "Injector finished: " + newPath;

            return newPath;
        }




        #region InjectAtCustomAttribute

        public bool TryGetCustomAttribute(MethodDefinition type, string attributeType, out CustomAttribute result)
        {
            result = null;
            if (!type.HasCustomAttributes)
                return false;

            foreach (CustomAttribute attribute in type.CustomAttributes)
            {
                if (attribute.Constructor.DeclaringType.FullName != attributeType)
                    continue;

                result = attribute;
                return true;
            }

            return false;
        }
        public bool InjectAtCustomAttribute(string CustomAttribute, string assemblyPath, string outPutFilePath)
        {
            // New assembly path
            string outputDirectory = outPutFilePath;
            return injectAtCustomAttribute(CustomAttribute, assemblyPath, outputDirectory);
        }
        private bool injectAtCustomAttribute(string CustomAttribute, string assemblyPath, string outputDirectory)
        {
            CustomAttribute customAttr;
            AssemblyDefinition asmDef;

            // New assembly path
            string fileName = Path.GetFileName(assemblyPath);

            // append date and time to new filename
            string newPath = Path.Combine(outputDirectory, DateTime.UtcNow.ToString("yyyy-MM-dd HH.mm.ss.fff", CultureInfo.InvariantCulture) + "_" + fileName);

            // Check if Output directory already exists, if not, create one
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            try
            {
                // Load assembly
                asmDef = AssemblyDefinition.ReadAssembly(assemblyPath);

                foreach (ModuleDefinition modDef in asmDef.Modules)
                {
                    foreach (TypeDefinition typDef in modDef.Types)
                    {
                        foreach (MethodDefinition metDef in typDef.Methods)
                        {
                            // Check if method has the required custom attribute set
                            //if (TryGetCustomAttribute(metDef, "AdvancedTracing.LogMethodExecutionEntryAttribute", out customAttr))
                            if (TryGetCustomAttribute(metDef, CustomAttribute, out customAttr))
                            {
                                #region TryGetCustomAttribute
                                // Method has the desired attribute set, edit IL for method
                                Trace.WriteLine("Found method " + metDef.ToString());

                                if (metDef.Parameters != null && metDef.Parameters.Count > 0)
                                {
                                    foreach (var param in metDef.Parameters)
                                    {
                                        string paramInfo = "metDef.Parameter: " + param.Name + " param.Resolve(): " + param.Resolve().ToString() + " " + param.ToString();
                                        Trace.WriteLine(paramInfo);
                                        Console.WriteLine(paramInfo);

                                    }
                                }

                                string variablesInfo = string.Empty;
                                if (metDef.Body != null && metDef.Body.Variables != null && metDef.Body.Variables.Count > 0)
                                {
                                    foreach (var variable in metDef.Body.Variables)
                                    {
                                        string varInfo = "            Variable - Type: " + variable.VariableType.ToString() + " Name: " + variable.Name + NewLine;
                                        varInfo += "            Index: " + variable.Index.ToString();
                                        Trace.WriteLine(varInfo);
                                        //Console.WriteLine(paramInfo);
                                        variablesInfo += varInfo;
                                    }
                                }

                                // Get ILProcessor
                                ILProcessor ilProcessor = metDef.Body.GetILProcessor();

                                // Load fully qualified method name as string and param + variable infos
                                Instruction i1 = ilProcessor.Create(
                                OpCodes.Ldstr,
                                metDef.ToString() + variablesInfo
                                );

                                // insert new load string instruction as first instruction in body
                                ilProcessor.InsertBefore(metDef.Body.Instructions[0], i1);

                                // Call the method which would write tracing info
                                Instruction i2 = ilProcessor.Create(
                                OpCodes.Call,
                                metDef.Module.Import(
                                typeof(Console).GetMethod("WriteLine", new[] { typeof(string) })
                                )
                                );
                                //insert second instruction after first
                                ilProcessor.InsertAfter(i1, i2);



                                ///
                                /// print parameters
                                ///


                                // import our pritObj Class
                                Type inputType = typeof(PrintObj);
                                TypeReference _printObj = asmDef.MainModule.Import(inputType);
                                MethodReference _printObjCtor = asmDef.MainModule.Import(inputType.GetConstructors().First(c => !c.IsStatic));


                                Type t = typeof(System.Reflection.MethodBase);
                                string methodname = "GetCurrentMethod";
                                MethodReference _methodGetCurrentMethod = asmDef.MainModule.Import(t.GetMethod(methodname));

                                methodname = "set_CurrentMethod";
                                MethodReference _methodSetMethod = asmDef.MainModule.Import(inputType.GetMethod(methodname));


                                t = typeof(System.Reflection.Assembly);
                                methodname = "GetExecutingAssembly";
                                MethodReference _methodGetExecutingAssembly = asmDef.MainModule.Import(t.GetMethod(methodname));

                                methodname = "set_CurrentAssembly";
                                MethodReference _methodSetExecutingAssembly = asmDef.MainModule.Import(inputType.GetMethod(methodname));


                                methodname = "get_CurrentArguments";
                                MethodReference _methodGetArguments = asmDef.MainModule.Import(inputType.GetMethod(methodname));

                                methodname = "set_CurrentArguments";
                                MethodReference _methodSetArguments = asmDef.MainModule.Import(inputType.GetMethod(methodname));



                                /*
                                VariableDefinition vInject = editor.AddVariable(injection.TypeReference);
                                VariableDefinition vInjection = editor.AddVariable(_cinjection);
                                VariableDefinition vObjectArray = editor.AddVariable(Assembly.ImportType<object[]>());
                                */

                                // Add new variables
                                metDef.Body.InitLocals = true;

                                VariableDefinition printO = new VariableDefinition(_printObj);
                                metDef.Body.Variables.Add(printO);

                                var objType = asmDef.MainModule.Import(typeof(object[]));
                                VariableDefinition oArray = new VariableDefinition(objType);
                                metDef.Body.Variables.Add(oArray);


                                // insert in reversed order as we insert always on top as first instruction

                                Instruction i6 = ilProcessor.Create(OpCodes.Ldloc_S, printO);
                                ilProcessor.InsertBefore(metDef.Body.Instructions[0], i6);

                                Instruction i5 = ilProcessor.Create(OpCodes.Ldc_I4, metDef.Parameters.Count);
                                ilProcessor.InsertBefore(metDef.Body.Instructions[0], i5);

                                Instruction i4 = ilProcessor.Create(OpCodes.Newarr, objType);
                                ilProcessor.InsertBefore(metDef.Body.Instructions[0], i4);

                                Instruction i3 = ilProcessor.Create(OpCodes.Stloc_S, oArray);
                                ilProcessor.InsertBefore(metDef.Body.Instructions[0], i3);

                                #endregion
                            }
                        }

                    }
                }
                // Save modified assembly
                asmDef.Write(newPath, new WriterParameters() { WriteSymbols = true });
            }
            catch
            {

            }

            return true;
        }

        #endregion

    }
}
