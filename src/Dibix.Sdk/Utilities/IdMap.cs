using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk
{
    internal sealed class IdMap<TFlag>
    {
        private readonly Type _type;

        private IdMap(Type type) => this._type = type;

        public static IdMap<TFlag> Create(Type type) => new IdMap<TFlag>(type);

        public string GetDefinitionName(TFlag flag) => FlagCache.GetFlagName(this._type, flag);

        public bool TryGetDefinitionName(TFlag flag, out string definitionName) => FlagCache.TryGetFlagName(this._type, flag, out definitionName);
    }

    internal abstract class IdMap<TType, TFlag>
    {
        public static string GetDefinitionName(TFlag flag) => FlagCache.GetFlagName(typeof(TType), flag);

        public static bool IsDefined(TFlag flag) => FlagCache.IsDefined(typeof(TType), flag);
    }

    internal static class FlagCache
    {
        private static readonly ConcurrentDictionary<Type, object> FlagTypeMap = new ConcurrentDictionary<Type, object>();

        public static string GetFlagName<TFlag>(Type type, TFlag flag) => GetFlagMap<TFlag>(type)[flag];

        public static bool TryGetFlagName<TFlag>(Type type, TFlag flag, out string definitionName) => GetFlagMap<TFlag>(type).TryGetValue(flag, out definitionName);
        
        public static bool IsDefined<TFlag>(Type type, TFlag flag) => GetFlagMap<TFlag>(type).ContainsKey(flag);

        private static IDictionary<TFlag, string> GetFlagMap<TFlag>(Type type) => (IDictionary<TFlag, string>)FlagTypeMap.GetOrAdd(type, CreateFlagMap<TFlag>);

        private static IDictionary<TFlag, string> CreateFlagMap<TFlag>(Type type) => CollectFlags<TFlag>(type).ToDictionary(x => x.Key, x => x.Value);

        private static IEnumerable<KeyValuePair<TFlag, string>> CollectFlags<TFlag>(IReflect type)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                object value = field.GetRawConstantValue();
                if (value is TFlag id)
                    yield return new KeyValuePair<TFlag, string>(id, field.Name);
            }
        }
    }
}