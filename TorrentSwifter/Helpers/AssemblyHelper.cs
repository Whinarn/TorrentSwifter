using System;
using System.Reflection;

namespace TorrentSwifter.Helpers
{
    internal static class AssemblyHelper
    {
        public static string GetAssemblyVersion(Type type)
        {
            var assembly = type.GetTypeInfo().Assembly;
            return GetAssemblyVersion(assembly);
        }

        public static string GetAssemblyVersion(Assembly assembly)
        {
            var assemblyName = assembly.GetName();
            return assemblyName.Version.ToString();
        }
    }
}
