using Logging;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using dnlib.DotNet.Emit;

namespace pTracer_dn
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


        
        private AssemblyResolver _assmeblyResolver;
        public AssemblyResolver _AssmeblyResolver
        {
            get { return _assmeblyResolver; }
            set
            {
                _assmeblyResolver = value;
                OnPropertyChanged("AssmeblyResolver");
            }
        }

        public void AddSearchPath(string Path)
        {
            if (!string.IsNullOrEmpty(Path) && Directory.Exists(Path))
            {
                if (_AssmeblyResolver == null) _AssmeblyResolver = new AssemblyResolver();
                _AssmeblyResolver.PostSearchPaths.Add(Path);
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
            AssemblyDef asmDef;

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
                    //MessageBox.Show(ex.ToString());
                    MainWindow.Instance.mBox("Injector Exception", ex.ToString());

                }
            }

            try
            {
                // AssemblyResolver
                if (_AssmeblyResolver == null) _AssmeblyResolver = new AssemblyResolver();
                if (Directory.Exists(Path.GetDirectoryName(assemblyPath))) AddSearchPath(Path.GetDirectoryName(assemblyPath));

                // how to use AssemblyResolver with dnLib?
                //_AssmeblyResolver

                // Load assembly
                //asmDef = AssemblyDef.Load(assemblyPath);
                ModuleDefMD mod = ModuleDefMD.Load(assemblyPath);

                // import our pritObj Class
                Importer importer = new Importer(mod);
                Type PrintObjType = typeof(PrintObj);
                ITypeDefOrRef _printObjTypeRef = importer.Import(PrintObjType);

                // This creates a new namespace Logging and class PrintObj in the new assembly, we don't want that
                //TypeDef _printObj = new TypeDefUser("Logging", "PrintObj", mod.CorLibTypes.Object.TypeDefOrRef);
                //var _printObjCtor = _printObj.FindDefaultConstructor();
                //mod.Types.Add(_printObj);



                Type t = typeof(System.Reflection.MethodBase);
                string methodname = "GetCurrentMethod";
                IMethod _methodGetCurrentMethod = importer.Import(t.GetMethod(methodname));

                methodname = "set_CurrentMethod";
                IMethod _methodSetMethod = importer.Import(PrintObjType.GetMethod(methodname));


                t = typeof(System.Reflection.Assembly);
                methodname = "GetExecutingAssembly";
                IMethod _methodGetExecutingAssembly = importer.Import(t.GetMethod(methodname));

                methodname = "set_CurrentAssembly";
                IMethod _methodSetExecutingAssembly = importer.Import(PrintObjType.GetMethod(methodname));


                methodname = "get_CurrentArguments";
                IMethod _methodGetArguments = importer.Import(PrintObjType.GetMethod(methodname));

                methodname = "set_CurrentArguments";
                IMethod _methodSetArguments = importer.Import(PrintObjType.GetMethod(methodname));

                methodname = "PrintArgs";
                IMethod _methodPrintArgs = importer.Import(PrintObjType.GetMethod(methodname));

                methodname = ".ctor";
                IMethod _printObjCtor = importer.Import(PrintObjType.GetMethod(methodname));

                foreach (ModuleDef modDef in mod.Assembly.Modules)
                {

                    foreach (TypeDef typDef in modDef.Types)
                    {
                        foreach (MethodDef metDef in typDef.Methods)
                        {
                            //if (MethodToken.Contains(metDef.MDToken.ToString()) && metDef.Name == "About1_Closed")
                            if (MethodToken.Contains(metDef.MDToken.ToString()))
                            {
                                if (WithTrace) Trace.WriteLine("Found method " + metDef.ToString() + " Token: " + metDef.MDToken.ToString());

                                try
                                {
                                    string variablesInfo = string.Empty;
                                    if (metDef.Body != null && metDef.Body.Variables != null && metDef.Body.Variables.Count > 0)
                                    {
                                        foreach (var variable in metDef.Body.Variables)
                                        {
                                            string varInfo = "            Variable - Type: " + variable.Type.ToString() + " Name: " + variable.Name + NewLine;
                                            varInfo += "            Index: " + variable.Index.ToString();
                                            if (WithTrace) Trace.WriteLine(varInfo);
                                            variablesInfo += varInfo;
                                        }
                                    }

                                    /*
                                     * if we want to skip anything 
                                    if (metDef.IsConstructor ||
                                        metDef.IsAbstract ||
                                        metDef.IsSetter ||
                                        (metDef.IsSpecialName && !metDef.IsGetter) || // to allow getter methods
                                        metDef.IsInstanceConstructor ||
                                        metDef.IsManaged == false
                                        )
                                    {
                                        if (WithTrace) Trace.WriteLine("Skipped unsupported metDef " + metDef.Name);
                                    }
                                    else if (metDef != null && metDef.Body != null)
                                    */

                                    if (metDef != null && metDef.Body != null)
                                    {

                                        var instructions = metDef.Body.Instructions;
                                        var newInstructions = new List<Instruction>();

                                        Instruction firstExistingInstruction = metDef.Body.Instructions[0];
                                        uint firstExistingInstrunctionOffset = firstExistingInstruction.Offset;
                                        int fIndex = (int)firstExistingInstrunctionOffset; // not working

                                        // nop Test
                                        //instructions.Insert((int)firstExistingInstruction.Offset, new Instruction(OpCodes.Nop));
                                        //instructions.Insert((int)firstExistingInstruction.Offset, new Instruction(OpCodes.Nop));

                                        ///
                                        /// Simple TraceLine
                                        ///

                                        // Load fully qualified method name as string
                                        //not working: (int)firstExistingInstruction.Offset

                                        //newInstructions.Add(new Instruction(OpCodes.Ldstr, metDef.ToString() + variablesInfo));
                                        //newInstructions.Add(new Instruction(OpCodes.Call, metDef.Module.Import(typeof(Trace).GetMethod("WriteLine", new[] { typeof(string) }))));



                                        ///
                                        /// PrintObj (injected Logging.dll)
                                        /// extended by using code and comments from CInject
                                        /// https://codeinject.codeplex.com/
                                        ///

                                        /*
                                        0	0000	nop
                                        1	0001	newobj	instance void Logging.PrintObj::.ctor()
                                        2	0006	stloc.0
                                        3	0007	ldloc.0
                                        4	0008	call	class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Reflection.MethodBase::GetCurrentMethod()
                                        5	000D	callvirt	instance void Logging.PrintObj::set_CurrentMethod(class [mscorlib]System.Reflection.MethodBase)
                                        6	0012	nop
                                        7	0013	ldloc.0
                                        8	0014	call	class [mscorlib]System.Reflection.Assembly [mscorlib]System.Reflection.Assembly::GetExecutingAssembly()
                                        9	0019	callvirt	instance void Logging.PrintObj::set_CurrentAssembly(class [mscorlib]System.Reflection.Assembly)
                                        10	001E	nop
                                        11	001F	ldloc.0
                                        12	0020	ldc.i4.2
                                        13	0021	newarr	[mscorlib]System.Object
                                        14	0026	dup
                                        15	0027	ldc.i4.0
                                        16	0028	ldstr	"Arg1"
                                        17	002D	stelem.ref
                                        18	002E	dup
                                        19	002F	ldc.i4.1
                                        20	0030	ldstr	"Arg2"
                                        21	0035	stelem.ref
                                        22	0036	callvirt	instance void Logging.PrintObj::set_CurrentArguments(object[])
                                        23	003B	nop
                                        24	003C	ldloc.0
                                        25	003D	callvirt	instance void Logging.PrintObj::PrintArgs()
                                        26	0042	nop
                                        */

                                        // Add new variables
                                        metDef.Body.InitLocals = true;

                                        Local printO = new Local(_printObjTypeRef.ToTypeSig());
                                        metDef.Body.Variables.Add(printO);
                                        
                                        

                                        var objType = mod.CorLibTypes.Object.ToTypeDefOrRef();
                                        var objTypeArr = importer.Import(typeof(object[]));
                                        Local oArray = new Local(objTypeArr.ToTypeSig());
                                        metDef.Body.Variables.Add(oArray);


                                        newInstructions.Add(new Instruction(OpCodes.Nop));

                                        // using MemberRef cTor will create the logging.PrintObj: new Logging.PrintObj()
                                        var objectCtor = new MemberRefUser(mod, ".ctor", MethodSig.CreateInstance(mod.CorLibTypes.Void), _printObjTypeRef);
                                        newInstructions.Add(new Instruction(OpCodes.Newobj, objectCtor));



                                        newInstructions.Add(OpCodes.Stloc.ToInstruction(printO));
                                        newInstructions.Add(OpCodes.Ldloc.ToInstruction(printO));

                                        newInstructions.Add(new Instruction(OpCodes.Call, _methodGetCurrentMethod));
                                        newInstructions.Add(new Instruction(OpCodes.Callvirt, _methodSetMethod));

                                        newInstructions.Add(new Instruction(OpCodes.Nop));

                                        newInstructions.Add(new Instruction(OpCodes.Ldloc_S, printO));

                                        // DNlib counts additionally hidden "this"
                                        List<Parameter> pList = new List<Parameter>();
                                        for (int i = 0; i < metDef.Parameters.Count; i++)
                                        {
                                            if (!metDef.Parameters[i].IsHiddenThisParameter) pList.Add(metDef.Parameters[i]);
                                        }

                                        newInstructions.Add(new Instruction(OpCodes.Ldc_I4, pList.Count));


                                        newInstructions.Add(new Instruction(OpCodes.Newarr, objType));
                                        newInstructions.Add(new Instruction(OpCodes.Stloc_S, oArray));


                                        //for (int i = 0; i < metDef.Parameters.Count; i++)
                                        for (int i = 0; i < pList.Count; i++)
                                        {
                                            if (WithTrace) Trace.WriteLine("Found Parameter " + pList[i].Name.ToString());

                                            bool processAsNormal = true;

                                            //if (metDef.Parameters[i].Type.IsByRef)
                                            if (pList[i].Type.IsByRef)
                                            {
                                                if (WithTrace) Trace.WriteLine("(IsByRef) " + pList[i].Name.ToString());

                                                //* Sample Instruction set:
                                                //* L_002a: ldloc.2 
                                                //* L_002b: ldc.i4.0 
                                                //* L_002c: ldarg.1 
                                                //* L_002d: ldind.ref 
                                                //* L_002e: stelem.ref 
                                                //* 

                                                newInstructions.Add(new Instruction(OpCodes.Ldloc_S, oArray));
                                                newInstructions.Add(new Instruction(OpCodes.Ldc_I4, i));

                                                newInstructions.Add(new Instruction(OpCodes.Ldarg, pList[i]));
                                                newInstructions.Add(new Instruction(OpCodes.Ldind_Ref));
                                                newInstructions.Add(new Instruction(OpCodes.Stelem_Ref));

                                                processAsNormal = false;
                                            }
                                            //else if (pList[i].IsHiddenThisParameter)
                                            //{
                                            //processAsNormal = false;
                                            //}

                                            else if (pList[i].Type.IsClassSig)
                                            {
                                                if (WithTrace) Trace.WriteLine("(IsClassSig) " + pList[i].Name.ToString() + " Type: " + pList[i].Type + " Type.ReflectionFullName: " + pList[i].Type.ReflectionFullName);

                                                newInstructions.Add(new Instruction(OpCodes.Ldloc_S, oArray));
                                                newInstructions.Add(new Instruction(OpCodes.Ldc_I4, i));
                                                newInstructions.Add(new Instruction(OpCodes.Ldarg, pList[i]));
                                                //newInstructions.Add(new Instruction(OpCodes.Box, pList[i].Type)); // causing System.InvalidCastException: Type "dnlib.DotNet.ClassSig" cannot be converted to Type "dnlib.DotNet.TypeSpec"

                                                ClassSig cSig = new ClassSig(pList[i].Type.ToTypeDefOrRef());
                                                Trace.WriteLine("(IsClassSig) cSig: " + cSig.ToString());

                                                newInstructions.Add(new Instruction(OpCodes.Stelem_Ref));

                                                processAsNormal = false;
                                            }
                                            else if (pList[i].Type.IsCorLibType)
                                            {
                                                if (WithTrace) Trace.WriteLine("(IsCorLibType) " + pList[i].Name.ToString() + " Type: " + pList[i].Type + " Type.ReflectionFullName: " + pList[i].Type.ReflectionFullName);
                                                if (WithTrace) Trace.WriteLine("(IsCorLibType...) " + " ElementType: " + pList[i].Type.ElementType + " Type.FullName: " + pList[i].Type.FullName);
                                                if (WithTrace) Trace.WriteLine("(IsCorLibType...) " + " Module: " + pList[i].Type.Module + " Type.Next: " + pList[i].Type.Next);
                                                if (WithTrace) Trace.WriteLine("(IsCorLibType...) " + " ReflectionName: " + pList[i].Type.ReflectionName + " Type.ReflectionNamespace: " + pList[i].Type.ReflectionNamespace);
                                                newInstructions.Add(new Instruction(OpCodes.Ldloc_S, oArray));
                                                newInstructions.Add(new Instruction(OpCodes.Ldc_I4, i));
                                                newInstructions.Add(new Instruction(OpCodes.Ldarg, pList[i]));
                                                
                                                //newInstructions.Add(new Instruction(OpCodes.Box, pList[i].Type)); // causing System.InvalidCastException: Type "dnlib.DotNet.CorLibTypeSig" cannot be converted to Type "dnlib.DotNet.TypeSpec"
                                                //newInstructions.Add(new Instruction(OpCodes.Box, mod.CorLibTypes.Int32)); // working for Int32 as example
                                                CorLibTypeSig cLibTypeSig = new CorLibTypeSig(pList[i].Type.ToTypeDefOrRef(), pList[i].Type.ElementType);
                                                newInstructions.Add(OpCodes.Box.ToInstruction(cLibTypeSig));
                                                
                                                newInstructions.Add(new Instruction(OpCodes.Stelem_Ref));

                                                processAsNormal = false;
                                            }


                                            //else if (metDef.Parameters[i].ParameterType.IsArray)
                                            //{

                                            //}
                                            //else if (metDef.Parameters[i].ParameterType.IsDefinition) // delegate needs no seperate handling
                                            //{

                                            //}
                                            //else if (metDef.Parameters[i].Type.IsFunctionPointer)
                                            else if (pList[i].Type.IsFunctionPointer)
                                            {
                                                if (WithTrace) Trace.WriteLine("(IsFunctionPointer) " + pList[i].Name.ToString());
                                            }

                                            //else if (metDef.Parameters[i].ParameterType.IsOptionalModifier)
                                            //{

                                            //}
                                            //else if (metDef.Parameters[i].Type.IsPointer)
                                            else if (pList[i].Type.IsPointer)
                                            {
                                                if (WithTrace) Trace.WriteLine("(IsPointer) " + pList[i].Name.ToString());
                                            }
                                            else
                                            {
                                                processAsNormal = true;
                                            }

                                            //if (processAsNormal && !metDef.Parameters[i].Type.IsClassSig && !metDef.Parameters[i].Type.IsCorLibType)
                                            if (processAsNormal)
                                            {
                                                if (WithTrace) Trace.WriteLine("processAsNormal: " + pList[i].Name.ToString());

                                                // Sample Instruction set: for simple PARAMETER
                                                //* L_0036: ldloc.s objArray
                                                //* L_0038: ldc.i4 0
                                                //* L_003d: ldarg array
                                                //* L_0041: box Int32    <-------------- anything can be here
                                                //* L_0046: stelem.ref 


                                                // Sample Instruction set: for ARRAY
                                                // L_0036: ldloc.s objArray
                                                // L_0038: ldc.i4 0
                                                // L_003d: ldarg array
                                                // L_0041: box string[]
                                                // L_0046: stelem.ref 


                                                newInstructions.Add(new Instruction(OpCodes.Ldloc_S, oArray));
                                                newInstructions.Add(new Instruction(OpCodes.Ldc_I4, i));
                                                newInstructions.Add(new Instruction(OpCodes.Ldarg, metDef.Parameters[i]));
                                                newInstructions.Add(new Instruction(OpCodes.Box, pList[i].Type));
                                                newInstructions.Add(new Instruction(OpCodes.Stelem_Ref));

                                            }
                                        }
                                        
                                        // fill Arguments array
                                        newInstructions.Add(new Instruction(OpCodes.Ldloc_S, oArray));
                                        newInstructions.Add(new Instruction(OpCodes.Callvirt, _methodSetArguments));

                                        // call PrintArgs
                                        newInstructions.Add(new Instruction(OpCodes.Ldloc_S, printO));
                                        newInstructions.Add(new Instruction(OpCodes.Callvirt, _methodPrintArgs));


                                        // Finally add instructions to beginning
                                        for (int j = 0; j < newInstructions.Count; j++)
                                        {
                                            instructions.Insert(j, newInstructions[j]);
                                        }


                                    }
                                    else
                                    {
                                        if (WithTrace) Trace.WriteLine("metDef or metDef.Body was null");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.ToString());
                                    MainWindow.Instance.mBox("Injector Exception", ex.ToString());
                                }

                            }
                        }

                    }
                }
                

                // Save modified assembly
                //asmDef.Write(newPath);

                var wopts = new dnlib.DotNet.Writer.ModuleWriterOptions(mod);
                wopts.WritePdb = true;

                //write assembly
                if (mod.IsILOnly) mod.Write(newPath);
                else mod.NativeWrite(newPath);

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


    }
}
