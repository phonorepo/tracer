using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Logging
{
    public class PrintObj
    {
        public System.Reflection.Assembly CurrentAssembly { get; set; }
        public System.Reflection.MethodBase CurrentMethod { get; set; }
        public object[] CurrentArguments { get; set; }

        public PrintObj()
        { }

        public PrintObj(System.Reflection.Assembly currentAssembly, System.Reflection.MethodBase currentMethod, object[] currentArguments)
        {
            CurrentAssembly = currentAssembly;
            CurrentMethod = currentMethod;
            CurrentArguments = currentArguments;
        }

        public void Tst()
        {
            PrintObj TestObject = new Logging.PrintObj();
            TestObject.CurrentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            TestObject.CurrentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            TestObject.CurrentArguments = new object[2] { "Arg1", "Arg2" };
            TestObject.PrintArgs();
        }


        private string GetStringValue(object input)
        {
            if (input == null)
                return "null";

            try
            {
                return CachedSerializer.Serialize(input.GetType(), input, Encoding.UTF8);
            }
            catch // can not serialize, then call ToString() method.
            {
                return input.ToString();
            }
        }

        public void PrintArgs()
        {
            string lineTag = "[InjectedTrace]";

            try
            {
                if (CurrentMethod == null)
                {
                    Trace.WriteLine(lineTag + "ERROR - PrintArgs: CurrentMethod not set");
                }
                else
                {
                    Trace.WriteLine(lineTag + DateTime.Now + ";" + CurrentMethod);

                    if (CurrentArguments != null && CurrentArguments.Length > 0)
                    {
                        Trace.WriteLine(lineTag + DateTime.Now + ";" + CurrentMethod + ";Arguments: " + CurrentArguments.Length);

                        ParameterInfo[] parameters = CurrentMethod.GetParameters();

                        for (int i = 0; i < CurrentArguments.Length; i++)
                        {
                            //Trace.WriteLine("i: " + i + " CurrentArguments.Length: " + CurrentArguments.Length);

                            var currentArgument = CurrentArguments[i];

                            if (currentArgument == null)
                            {
                                if (parameters != null && parameters.Length > 0)
                                {
                                    Trace.WriteLine(lineTag + "Pa: " + parameters[i].Name);
                                }
                                else
                                {
                                    Trace.WriteLine(lineTag + "Pa: is null");
                                }
                                continue;
                            }

                            if (currentArgument is IDictionary)
                            {
                                var dictionary = (IDictionary)currentArgument;
                                var dictionaryBuilder = new StringBuilder();
                                foreach (var key in dictionary.Keys)
                                {
                                    dictionaryBuilder.AppendFormat("{0}={1}|", key, GetStringValue(dictionary[key]));
                                }

                                if (parameters != null && parameters.Length > 0) Trace.WriteLine(String.Format(lineTag + "    [{0}]: {1}", parameters[i].Name, dictionaryBuilder.ToString().TrimEnd(new[] { '|' })));
                            }
                            else if (currentArgument is ICollection)
                            {
                                ICollection collection = (ICollection)currentArgument;
                                IEnumerator enumerator = collection.GetEnumerator();
                                StringBuilder dictionaryBuilder = new StringBuilder();

                                while (enumerator.MoveNext())
                                {
                                    dictionaryBuilder.AppendFormat("{0},", GetStringValue(enumerator.Current)).AppendLine();
                                }

                                if (parameters != null && parameters.Length > 0) Trace.WriteLine(String.Format(lineTag + "    [{0}]: {1}", parameters[i].Name, dictionaryBuilder.ToString().TrimEnd(new[] { ',' })));
                            }
                            else if (currentArgument is String)
                            {
                                if (parameters != null && parameters.Length > 0) Trace.WriteLine(String.Format(lineTag + "    [{0}]: {1}", parameters[i].Name, currentArgument.ToString()));
                            }
                            else if (currentArgument is IEnumerable)
                            {
                                IEnumerable enumerator = (IEnumerable)currentArgument;
                                StringBuilder dictionaryBuilder = new StringBuilder();

                                foreach (var item in enumerator)
                                {
                                    dictionaryBuilder.AppendFormat("{0},", GetStringValue(item)).AppendLine();
                                }
                                if (parameters != null && parameters.Length > 0) Trace.WriteLine(String.Format(lineTag + "    [{0}]: {1}", parameters[i].Name, dictionaryBuilder.ToString().TrimEnd(new[] { ',' })));
                            }
                            else
                            {
                                if (parameters != null && parameters.Length > 0) Trace.WriteLine(String.Format(lineTag + "    [{0}]: {1}", parameters[i].Name, GetStringValue(currentArgument)));
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception);
            }
        }

    }
}
