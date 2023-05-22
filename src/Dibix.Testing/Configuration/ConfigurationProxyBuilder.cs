using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Configuration.Dibix;

namespace Dibix.Testing
{
    internal static class ConfigurationProxyBuilder
    {
        private static readonly Type PropertyInitializationTrackerType = typeof(ConfigurationPropertyInitializationTracker);
        private static readonly ConstructorInfo PropertyInitializationTrackerCtor = PropertyInitializationTrackerType.GetConstructorSafe(typeof(ConfigurationInitializationToken));
        private static readonly MethodInfo PropertyInitializationTrackerEnterSectionMethod = PropertyInitializationTrackerType.SafeGetMethod(nameof(ConfigurationPropertyInitializationTracker.EnterSection), new[] { typeof(string) });
        private static readonly MethodInfo PropertyInitializationTrackerVerifyMethod = PropertyInitializationTrackerType.SafeGetMethod(nameof(ConfigurationPropertyInitializationTracker.Verify));
        private static readonly MethodInfo PropertyInitializationTrackerInitializeMethod = PropertyInitializationTrackerType.SafeGetMethod(nameof(ConfigurationPropertyInitializationTracker.Initialize));

        private static readonly ConfigurationProxyDecisionStrategy Strategy = new VirtualMemberConfigurationProxyDecisionStrategy();
        //private static readonly ConfigurationProxyDecisionStrategy Strategy = new RequiredAttributeConfigurationProxyDecisionStrategy();
        //private static readonly ConfigurationProxyDecisionStrategy Strategy = new InitializerConfigurationProxyDecisionStrategy();
        private static readonly ConcurrentDictionary<AssemblyName, ModuleBuilder> ModuleBuilderMap = new ConcurrentDictionary<AssemblyName, ModuleBuilder>();
        private static readonly ConcurrentDictionary<Type, Type> TypeMap = new ConcurrentDictionary<Type, Type>();

        public static T BuildProxyIfNeeded<T>(ConfigurationInitializationToken initializationToken) where T : new()
        {
            Type type = typeof(T);
            Type proxyType = GetProxyType(type);
            T proxyInstance = type == proxyType ? new T() : (T)Activator.CreateInstance(proxyType, initializationToken);
            return proxyInstance;
        }

        private static Type GetProxyType(Type type, ModuleBuilder moduleBuilder = null)
        {
            if (TypeMap.TryGetValue(type, out Type proxyType))
                return proxyType;

            proxyType = BuildProxyType(type, moduleBuilder);
            TypeMap.TryAdd(type, proxyType);
            return proxyType;
        }

        private static Type BuildProxyType(Type type, ModuleBuilder moduleBuilder = null)
        {
            ConfigurationProxyLookup lookup = Strategy.Collect(type);
            if (!Strategy.ShouldCreateProxy(lookup))
                return type;

            if (type.IsSealed)
                throw new InvalidOperationException($"Configuration class must not be sealed: {type}");

            if (moduleBuilder == null)
            {
                AssemblyName assemblyName = type.Assembly.GetName();
                moduleBuilder = ModuleBuilderMap.GetOrAdd(assemblyName, CreateModuleBuilder);
            }

            TypeBuilder typeBuilder = moduleBuilder.DefineType(type.FullName, attr: TypeAttributes.NotPublic | TypeAttributes.Sealed, parent: type);

            // private readonly ConfigurationPropertyInitializationTracker _propertyInitializationTracker;
            FieldBuilder propertyInitializationTrackerField = typeBuilder.DefineField("_propertyInitializationTracker", PropertyInitializationTrackerType, FieldAttributes.Private | FieldAttributes.InitOnly);

            const MethodAttributes ctorAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            ConstructorBuilder constructor = typeBuilder.DefineConstructor(ctorAttributes, callingConvention: default, parameterTypes: new[] { typeof(ConfigurationInitializationToken) });
            ILGenerator ctorIL = constructor.GetILGenerator();

            // Call base ctor
            ctorIL.Emit(OpCodes.Ldarg_0); // this
            ctorIL.Emit(OpCodes.Call, type.GetConstructorSafe(Type.EmptyTypes)); // this
            ctorIL.Emit(OpCodes.Nop);
            
            ctorIL.Emit(OpCodes.Nop);

            // this._propertyInitializationTracker = new ConfigurationPropertyInitializationTracker(propertyName);
            ctorIL.Emit(OpCodes.Ldarg_0); // this
            ctorIL.Emit(OpCodes.Ldarg_1); // 'initializationToken'
            ctorIL.Emit(OpCodes.Newobj, PropertyInitializationTrackerCtor);
            ctorIL.Emit(OpCodes.Stfld, propertyInitializationTrackerField);

            // Dynamic properties
            foreach (PropertyInfo property in lookup.PrimitiveProperties)
                DefinePrimitiveProperty(property, typeBuilder, propertyInitializationTrackerField);
            
            foreach (PropertyInfo property in lookup.ComplexProperties.Keys)
                DefineComplexProperty(property, moduleBuilder, typeBuilder, ctorIL);

            ctorIL.Emit(OpCodes.Ret);

            // IConfigurationSectionHandler.EnterSection
            typeBuilder.AddInterfaceImplementation(typeof(IConfigurationSectionHandler));
            MethodInfo enterSectionMethod = typeof(IConfigurationSectionHandler).SafeGetMethod(nameof(IConfigurationSectionHandler.EnterSection), new[] { typeof(string) });
            MethodBuilder enterSectionMethodImpl = typeBuilder.DefineMethod(enterSectionMethod.Name, MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot, callingConvention: default, enterSectionMethod.ReturnType, new[] { typeof(string) });
            ILGenerator enterSectionMethodIL = enterSectionMethodImpl.GetILGenerator();
            enterSectionMethodIL.Emit(OpCodes.Ldarg_0); // this
            enterSectionMethodIL.Emit(OpCodes.Ldfld, propertyInitializationTrackerField);
            enterSectionMethodIL.Emit(OpCodes.Ldarg_1); // path
            enterSectionMethodIL.Emit(OpCodes.Callvirt, PropertyInitializationTrackerEnterSectionMethod);
            enterSectionMethodIL.Emit(OpCodes.Nop);
            enterSectionMethodIL.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(enterSectionMethodImpl, enterSectionMethod);

            Type proxyType = typeBuilder.CreateType();
            return proxyType;
        }

        private static void DefinePrimitiveProperty(PropertyInfo property, TypeBuilder typeBuilder, FieldInfo propertyInitializationTrackerField)
        {
            Type propertyType = property.PropertyType;

            void GetterBody(ILGenerator getterIL)
            {
                // this._propertyInitializationTracker.Verify("Property");
                getterIL.Emit(OpCodes.Ldarg_0); // this
                getterIL.Emit(OpCodes.Ldfld, propertyInitializationTrackerField);
                getterIL.Emit(OpCodes.Ldstr, property.Name); // 'Property'
                getterIL.Emit(OpCodes.Callvirt, PropertyInitializationTrackerVerifyMethod);
                getterIL.Emit(OpCodes.Nop);

                // return base.Property;
                getterIL.Emit(OpCodes.Ldarg_0); // this
                getterIL.Emit(OpCodes.Call, property.GetMethod);
                getterIL.Emit(OpCodes.Ret);
            }

            void SetterBody(ILGenerator setterIL)
            {
                // base.Property = value;
                setterIL.Emit(OpCodes.Ldarg_0); // this
                setterIL.Emit(OpCodes.Ldarg_1); // 'value'
                setterIL.Emit(OpCodes.Call, property.SetMethod);
                setterIL.Emit(OpCodes.Nop);

                // this._propertyInitializationTracker.Initialize("Property");
                setterIL.Emit(OpCodes.Ldarg_0); // this
                setterIL.Emit(OpCodes.Ldfld, propertyInitializationTrackerField);
                setterIL.Emit(OpCodes.Ldstr, property.Name);
                setterIL.Emit(OpCodes.Callvirt, PropertyInitializationTrackerInitializeMethod);
                setterIL.Emit(OpCodes.Nop);

                setterIL.Emit(OpCodes.Ret);
            }

            DefineProperty(typeBuilder, property.Name, propertyType, GetterBody, SetterBody, property);
        }

        private static void DefineComplexProperty(PropertyInfo property, ModuleBuilder moduleBuilder, TypeBuilder typeBuilder, ILGenerator ctorIL)
        {
            Type propertyType = property.PropertyType;
            Type proxyType = GetProxyType(property.PropertyType, moduleBuilder);

            FieldBuilder underlyingInstanceField = typeBuilder.DefineField($"<{property.Name}>k__BackingField", propertyType, FieldAttributes.Private | FieldAttributes.InitOnly);

            // this.Property = new ConfigurationProxy(initializationToken);
            ctorIL.Emit(OpCodes.Ldarg_0); // this
            ctorIL.Emit(OpCodes.Ldarg_1); // 'initializationToken'
            ctorIL.Emit(OpCodes.Newobj, proxyType.GetConstructorSafe(typeof(ConfigurationInitializationToken)));
            ctorIL.Emit(OpCodes.Stfld, underlyingInstanceField);

            void GetterBody(ILGenerator getterIL)
            {
                // return this.Property;
                getterIL.Emit(OpCodes.Ldarg_0); // this
                getterIL.Emit(OpCodes.Ldfld, underlyingInstanceField);
                getterIL.Emit(OpCodes.Ret);
            }

            DefineProperty(typeBuilder, property.Name, propertyType, GetterBody, setterBody: null, property);
        }

        private static void DefineProperty(TypeBuilder typeBuilder, string name, Type returnType, Action<ILGenerator> getterBody, Action<ILGenerator> setterBody, PropertyInfo baseProperty = null)
        {
            PropertyBuilder property = typeBuilder.DefineProperty(name, attributes: default, returnType, parameterTypes: null);
            MethodAttributes getSetAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;

            if (baseProperty != null)
            {
                getSetAttributes |= MethodAttributes.Virtual;

                if ((baseProperty.DeclaringType?.IsInterface).GetValueOrDefault(false))
                    getSetAttributes |= MethodAttributes.Final | MethodAttributes.NewSlot;
            }

            MethodBuilder getter = typeBuilder.DefineMethod($"get_{name}", getSetAttributes, returnType, parameterTypes: null);
            ILGenerator getterIL = getter.GetILGenerator();
            getterBody(getterIL);
            property.SetGetMethod(getter);

            if (baseProperty != null)
            {
                if (!baseProperty.GetMethod.IsVirtual)
                    ThrowPropertyNotVirtual(baseProperty);

                typeBuilder.DefineMethodOverride(getter, baseProperty.GetMethod);
            }

            if (setterBody != null)
            {
                MethodBuilder setter = typeBuilder.DefineMethod($"set_{property.Name}", getSetAttributes, returnType: typeof(void), parameterTypes: new[] { returnType });
                ILGenerator setterIL = setter.GetILGenerator();
                setterBody(setterIL);
                property.SetSetMethod(setter);

                if (baseProperty != null)
                {
                    if (!baseProperty.SetMethod.IsVirtual)
                        ThrowPropertyNotVirtual(baseProperty);

                    typeBuilder.DefineMethodOverride(setter, baseProperty.SetMethod);
                }
            }
        }

        private static ModuleBuilder CreateModuleBuilder(AssemblyName assemblyName)
        {
            AssemblyName proxyAssemblyName = new AssemblyName(assemblyName.FullName);
            proxyAssemblyName.Name = $"{assemblyName.Name}.Proxy";
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(proxyAssemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(proxyAssemblyName.Name);
            return moduleBuilder;
        }

        private static void ThrowPropertyNotVirtual(PropertyInfo property) => throw new InvalidOperationException($"Configuration property must be virtual: {property.DeclaringType}.{property.Name}");
    }
}