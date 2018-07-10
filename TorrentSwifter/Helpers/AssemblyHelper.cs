using System;
using System.Reflection;

namespace TorrentSwifter.Helpers
{
    internal static class AssemblyHelper
    {
        public static Version GetAssemblyVersion(Type type)
        {
            var assembly = type.GetTypeInfo().Assembly;
            return GetAssemblyVersion(assembly);
        }

        public static Version GetAssemblyVersion(Assembly assembly)
        {
            var assemblyName = assembly.GetName();
            return assemblyName.Version;
        }
    }
}
