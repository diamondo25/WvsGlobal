using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;

namespace WvsBeta.Game
{
    public class Scripting
    {
        private static CodeDomProvider compiler = CodeDomProvider.CreateProvider("CSharp");
        public static CompilerResults CompileScript(string Source)
        {
            CompilerParameters parms = new CompilerParameters()
            {

                // Configure parameters
                GenerateExecutable = false,
                GenerateInMemory = true,
                IncludeDebugInformation = false
            };

            var mainPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (var r in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
            {
                if (File.Exists(Path.Combine(mainPath, r.Name + ".dll")))
                    parms.ReferencedAssemblies.Add(Path.Combine(mainPath, r.Name + ".dll"));
                else
                    parms.ReferencedAssemblies.Add(r.Name + ".dll");
            }
            parms.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);

            return compiler.CompileAssemblyFromFile(parms, Source);
        }

        public static object FindInterface(System.Reflection.Assembly DLL, string InterfaceName)
        {
            // Loop through types looking for one that implements the given interface
            foreach (Type t in DLL.GetTypes())
            {
                if (t.GetInterface(InterfaceName, true) != null)
                    return DLL.CreateInstance(t.FullName);
            }

            return null;
        }
    }
}