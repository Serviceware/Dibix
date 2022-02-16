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

        public string GetName(TFlag flag) => FlagMap.GetFlagName(this._type, flag);

        public bool TryGetName(TFlag flag, out string definitionName) => FlagMap.TryGetFlagName(this._type, flag, out definitionName);
    }

    internal abstract class IdMap<TType, TFlag>
    {
        public static string GetName(TFlag flag) => FlagMap.GetFlagName<TType, TFlag>(flag);
        public static bool IsDefined(TFlag flag) => FlagMap.IsDefined<TType, TFlag>(flag);
    }

    internal static class FlagMap
    {
        public static string GetFlagName<TType, TFlag>(TFlag value) => GetFlagName(typeof(TType), value);
        public static string GetFlagName<TFlag>(Type type, TFlag value)
        {
            FlagDescriptorCache<TFlag> flagDescriptorCache = FlagTypeCache.GetFlagCache<TFlag>(type);
            return flagDescriptorCache.GetDescriptorByValue(value).Name;
        }

        public static bool TryGetFlagName<TFlag>(Type type, TFlag flag, out string definitionName)
        {
            FlagDescriptorCache<TFlag> flagDescriptorCache = FlagTypeCache.GetFlagCache<TFlag>(type);
            if (flagDescriptorCache.TryGetDescriptorByValue(flag, out FlagDescriptor<TFlag> descriptor))
            {
                definitionName = descriptor.Name;
                return true;
            }

            definitionName = null;
            return false;
        }

        public static bool IsDefined<TType, TFlag>(TFlag value)
        {
            FlagDescriptorCache<TFlag> flagDescriptorCache = FlagTypeCache.GetFlagCache<TFlag>(typeof(TType));
            return flagDescriptorCache.IsValueDefined(value);
        }
    }

    internal static class FlagTypeCache
    {
        private static readonly ConcurrentDictionary<Type, object> FlagTypeMap = new ConcurrentDictionary<Type, object>();

        public static FlagDescriptorCache<TFlag> GetFlagCache<TFlag>(Type type) => (FlagDescriptorCache<TFlag>)FlagTypeMap.GetOrAdd(type, CreateFlagDescriptorCache<TFlag>);

        private static object CreateFlagDescriptorCache<TFlag>(Type type) => FlagDescriptorCache<TFlag>.Create(type);
    }

    internal sealed class FlagDescriptorCache<TFlag>
    {
        private readonly IDictionary<string, FlagDescriptor<TFlag>> _nameMap;
        private readonly IDictionary<TFlag, FlagDescriptor<TFlag>> _valueMap;

        public Type Type { get; }

        private FlagDescriptorCache(Type type, ICollection<FlagDescriptor<TFlag>> descriptors)
        {
            this.Type = type;
            this._nameMap = descriptors.ToDictionary(x => x.Name);
            this._valueMap = descriptors.ToDictionary(x => x.Value);
        }

        public FlagDescriptor<TFlag> GetDescriptorByName(string name) => this._nameMap[name];
        public FlagDescriptor<TFlag> GetDescriptorByValue(TFlag value) => this._valueMap[value];

        public bool TryGetDescriptorByName(string name, out FlagDescriptor<TFlag> descriptor) => this._nameMap.TryGetValue(name, out descriptor);
        public bool TryGetDescriptorByValue(TFlag value, out FlagDescriptor<TFlag> descriptor) => this._valueMap.TryGetValue(value, out descriptor);

        public bool IsNameDefined(string name) => this._nameMap.ContainsKey(name);
        public bool IsValueDefined(TFlag value) => this._valueMap.ContainsKey(value);

        public static FlagDescriptorCache<TFlag> Create(Type type)
        {
            ICollection<FlagDescriptor<TFlag>> descriptors = CollectFlags(type).ToArray();
            return new FlagDescriptorCache<TFlag>(type, descriptors);
        }

        private static IEnumerable<FlagDescriptor<TFlag>> CollectFlags(IReflect type)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                object value = field.GetRawConstantValue();
                if (value is TFlag id)
                    yield return new FlagDescriptor<TFlag>(field.Name, id);
            }
        }
    }

    internal sealed class FlagDescriptor<TFlag>
    {
        public string Name { get; }
        public TFlag Value { get; }

        public FlagDescriptor(string name, TFlag value)
        {
            this.Name = name;
            this.Value = value;
        }
    }
}