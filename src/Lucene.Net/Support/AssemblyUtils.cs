﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if NETSTANDARD
using Microsoft.Extensions.DependencyModel;
#endif

namespace Lucene.Net.Support
{
    /// <summary>
    /// Methods for working with Assemblies.
    /// </summary>
    public class AssemblyUtils
    {
        /// <summary>
        /// Gets a list of the host assembly's referenced assemblies excluding 
        /// any Microsoft, System, or Mono prefixed assemblies or assemblies with
        /// official Microsoft key hashes. Essentially, we get a list of all non
        /// Microsoft assemblies here.
        /// </summary>
        /// <returns></returns>
        public static IList<Assembly> GetReferencedAssemblies()
        {
            // .NET Port Hack: We do a 2-level deep check here because if the assembly you're
            // hoping would be loaded hasn't been loaded yet into the app domain,
            // it is unavailable. So we go to the next level on each and check each referenced
            // assembly.
#if NETSTANDARD
            var dependencyContext = DependencyContext.Default;
            var assemblyNames = dependencyContext.RuntimeLibraries
                .SelectMany(lib => lib.GetDefaultAssemblyNames(dependencyContext))
                .Where(x => !DotNetFrameworkFilter.IsFrameworkAssembly(x))
                .Distinct();
            var assembliesLoaded = LoadAssemblyFromName(assemblyNames);
#else
            var assembliesLoaded = AppDomain.CurrentDomain.GetAssemblies();
#endif
            assembliesLoaded = assembliesLoaded.Where(x => !DotNetFrameworkFilter.IsFrameworkAssembly(x)).ToArray();

            var referencedAssemblies = assembliesLoaded
                .SelectMany(assembly =>
                {
                    return assembly
                        .GetReferencedAssemblies()
                        .Where(reference => !DotNetFrameworkFilter.IsFrameworkAssembly(reference))
                        .Select(assemblyName => LoadAssemblyFromName(assemblyName));
                })
                .Where(x => x != null)
                .Distinct();

            return assembliesLoaded.Concat(referencedAssemblies).Distinct().ToList();
        }

        private static IEnumerable<Assembly> LoadAssemblyFromName(IEnumerable<AssemblyName> assemblyNames)
        {
            return assemblyNames.Select(x => LoadAssemblyFromName(x)).Where(x => x != null);
        }

        private static Assembly LoadAssemblyFromName(AssemblyName assemblyName)
        {
            try
            {
                return Assembly.Load(assemblyName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Assembly filter logic from:
        /// https://raw.githubusercontent.com/Microsoft/dotnet-apiport/master/src/Microsoft.Fx.Portability/Analyzer/DotNetFrameworkFilter.cs
        /// </summary>
        public static class DotNetFrameworkFilter
        {
            /// <summary>
            /// These keys are a collection of public key tokens derived from all the reference assemblies in
            /// "%ProgramFiles%\Reference Assemblies\Microsoft" on a Windows 10 machine with VS 2015 installed
            /// </summary>
            private static readonly ICollection<string> s_microsoftKeys = new HashSet<string>(new[]
            {
                "b77a5c561934e089", // ECMA
                "b03f5f7f11d50a3a", // DEVDIV
                "7cec85d7bea7798e", // SLPLAT
                "31bf3856ad364e35", // Windows
                "24eec0d8c86cda1e", // Phone
                "0738eb9f132ed756", // Mono
                "ddd0da4d3e678217", // Component model
                "84e04ff9cfb79065", // Mono Android
                "842cf8be1de50553"  // Xamarin.iOS
            }, StringComparer.OrdinalIgnoreCase);

            private static readonly IEnumerable<string> s_frameworkAssemblyNamePrefixes = new[]
            {
                "System.",
                "Microsoft.",
                "Mono."
            };

            /// <summary>
            /// Gets a best guess as to whether this assembly is a .NET Framework assembly or not.
            /// </summary>
            public static bool IsFrameworkAssembly(Assembly assembly)
            {
                return assembly != null && IsFrameworkAssembly(assembly.GetName());
            }

            /// <summary>
            /// Gets a best guess as to whether this assembly is a .NET Framework assembly or not.
            /// </summary>
            public static bool IsFrameworkAssembly(AssemblyName assembly)
            {
                if (assembly == null)
                {
                    return false;
                }

                if (s_frameworkAssemblyNamePrefixes.Any(p => assembly.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                var publicKey = assembly.GetPublicKeyToken();

                if (publicKey == default(byte[]))
                {
                    return false;
                }

                var publicKeyToken = string.Concat(publicKey.Select(i => i.ToString("x2")));

                return s_microsoftKeys.Contains(publicKeyToken);
            }
        }
    }
}
