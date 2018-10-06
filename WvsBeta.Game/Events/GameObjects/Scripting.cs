using System;
using System.CodeDom.Compiler;
using System.Reflection;

namespace WvsBeta.Game {
    public class Scripting
    {
        private static CodeDomProvider compiler = CodeDomProvider.CreateProvider("CSharp");
        private static CompilerParameters parms = null;
		public static CompilerResults CompileScript(string Source) {
            if (parms == null)
            {
                parms = new CompilerParameters();

                // Configure parameters
                parms.GenerateExecutable = false;
                parms.GenerateInMemory = true;
                parms.IncludeDebugInformation = false;
                foreach (var r in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                {
                    parms.ReferencedAssemblies.Add(r.Name + ".dll");
                }
                parms.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            }

			return compiler.CompileAssemblyFromFile(parms, Source);
		}

		public static object FindInterface(System.Reflection.Assembly DLL, string InterfaceName) {
			// Loop through types looking for one that implements the given interface
			foreach (Type t in DLL.GetTypes()) {
				if (t.GetInterface(InterfaceName, true) != null)
					return DLL.CreateInstance(t.FullName);
			}

			return null;
		}
	}
}
