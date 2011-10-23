using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Zepheus.Util
{
    public static class Reflector
    {
        public static IEnumerable<Pair<TAttribute, MethodInfo>> FindMethodsByAttribute<TAttribute>()
            where TAttribute : Attribute
        {
            return (from method in AppDomain.CurrentDomain.GetAssemblies()
                        .Where(assembly => !assembly.GlobalAssemblyCache)
                        .SelectMany(assembly => assembly.GetTypes())
                        .SelectMany(type => type.GetMethods())
                    let attribute = Attribute.GetCustomAttribute(method, typeof(TAttribute), false) as TAttribute
                    where attribute != null
                    select new Pair<TAttribute, MethodInfo>(attribute, method));
        }

        public static IEnumerable<Func<bool>> GetInitializerMethods()
        {
            return (from assembly in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.GlobalAssemblyCache)
                    from type in assembly.GetTypes()
                    let serverModuleAttribute = Attribute.GetCustomAttribute(type, typeof(ServerModuleAttribute)) as ServerModuleAttribute
                    where serverModuleAttribute != null
                    from method in type.GetMethods()
                    let initMethodAttribute = Attribute.GetCustomAttribute(method, typeof(InitializerMethodAttribute)) as InitializerMethodAttribute
                    where initMethodAttribute != null
                    orderby serverModuleAttribute.InitializationStage ascending
                    select (Func<bool>) Delegate.CreateDelegate(typeof(Func<bool>), method));
        }
    }
}
