using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace Dan200.Launcher.Util
{
    public class EmbeddedAssembly
    {
        static Dictionary<string, Assembly> s_assemblies = new Dictionary<string, Assembly>();

        public static void Load( string resourceName )
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            using( var stream = currentAssembly.GetManifestResourceStream( resourceName ) )
            {
                // Read the resource into bytes
                byte[] bytes = new byte[ (int)stream.Length ];
                int pos = 0;
                while( pos < bytes.Length )
                {
                    pos += stream.Read( bytes, pos, bytes.Length - pos );
                }

                // Load the assembly from the bytes
                var assembly = Assembly.Load( bytes );
                s_assemblies.Add( assembly.FullName, assembly );
            }
        }

        public static Assembly Get( string assemblyFullName )
        {
            if( s_assemblies.ContainsKey( assemblyFullName ) )
            {
                return s_assemblies[ assemblyFullName ];
            }
            return null;
        }
    }
}