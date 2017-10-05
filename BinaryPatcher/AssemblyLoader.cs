﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace BinaryPatcher
{
    public static class AssemblyLoader
    {
        internal static Dictionary<string, Assembly> myDict;

        public static Assembly LoadOnce(string assemblyName)
        {
            if (myDict == null)
                myDict = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

            if (myDict.ContainsKey(assemblyName))
                return myDict[assemblyName];
            else
            {
                byte[] bytes;
                string resourceName = "BinaryPatcher.Dlls." + assemblyName + ".dll";
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                using (Stream stream = currentAssembly.GetManifestResourceStream(resourceName))
                {
                    bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                }
                Assembly result = Assembly.Load(bytes);
                myDict.Add(assemblyName, result);
                bytes = null;
                return result;
            }
        }

        public static Assembly AssemblyResolve(object sender, ResolveEventArgs e)
        {
            if (myDict == null)
                myDict = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            string RealName = e.Name.Split(',')[0].Trim();
            if (myDict.ContainsKey(RealName))
                return myDict[RealName];
            else
            {
                byte[] bytes;
                string resourceName = "BinaryPatcher.Dlls." + RealName + ".dll";
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                using (Stream stream = currentAssembly.GetManifestResourceStream(resourceName))
                {
                    bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                }
                Assembly result = Assembly.Load(bytes);
                myDict.Add(RealName, result);
                bytes = null;
                return result;
            }

        }
    }
}
