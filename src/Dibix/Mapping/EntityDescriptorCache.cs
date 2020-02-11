﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dibix
{
    internal static class EntityDescriptorCache
    {
        private static readonly ConcurrentDictionary<Type, EntityDescriptor> Cache = new ConcurrentDictionary<Type, EntityDescriptor>();

        public static EntityDescriptor GetDescriptor(Type type) => Cache.GetOrAdd(type, BuildDescriptor);

        private static EntityDescriptor BuildDescriptor(Type type)
        {
            EntityDescriptor descriptor = new EntityDescriptor();
            if (type.IsPrimitive())
                return descriptor;

            foreach (PropertyInfo property in type.GetRuntimeProperties())
            {
                if (property.IsDefined(typeof(KeyAttribute)))
                {
                    descriptor.Keys.Add(BuildEntityKey(property));
                }
                else if (property.IsDefined(typeof(DiscriminatorAttribute)))
                {
                    if (descriptor.Discriminator != null)
                        throw new InvalidOperationException($"Composite discriminator keys are not supported: {type}");

                    descriptor.Discriminator = BuildEntityKey(property);
                }
                else
                {
                    EntityProperty entityProperty = BuildEntityProperty(property);
                    descriptor.Properties.Add(entityProperty);
                }

                if (property.IsDefined(typeof(ObfuscatedAttribute)))
                    descriptor.ObfuscatedProperties.Add(BuildObfuscatedPropertyAccessor(property));
            }

            if (descriptor.Discriminator != null && descriptor.Keys.Count != 1)
                throw new InvalidOperationException($"To match a discriminator, exactly one key property should be defined: {type}");

            return descriptor;
        }

        private static EntityKey BuildEntityKey(PropertyInfo property)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
            Expression instanceCast = Expression.Convert(instanceParameter, property.DeclaringType);
            Expression propertyAccessor = Expression.Property(instanceCast, property);
            Expression propertyAccessorCast = Expression.Convert(propertyAccessor, typeof(object));
            Expression<Func<object, object>> expression = Expression.Lambda<Func<object, object>>(propertyAccessorCast, instanceParameter);
            Func<object, object> compiled = expression.Compile();
            return new EntityKey(compiled);
        }

        private static EntityProperty BuildEntityProperty(PropertyInfo property)
        {
            if (!TryGetCollectionType(property.PropertyType, out Type collectionType))
                return BuildNonCollectionEntityProperty(property);

            return BuildCollectionEntityProperty(property, collectionType);
        }

        private static EntityProperty BuildNonCollectionEntityProperty(PropertyInfo property)
        {
            Type entityType = property.PropertyType;
            return BuildEntityProperty(property, entityType, false, Expression.Assign);
        }

        private static EntityProperty BuildCollectionEntityProperty(PropertyInfo propertyInfo, Type collectionType)
        {
            Type entityType = collectionType.GenericTypeArguments[0];
            return BuildEntityProperty(propertyInfo, entityType, true, (property, value) => Expression.Call(property, collectionType.GetTypeInfo().GetDeclaredMethod("Add"), value));
        }

        private static EntityProperty BuildEntityProperty(PropertyInfo property, Type entityType, bool isCollection, Func<Expression, Expression, Expression> valueSetter)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
            ParameterExpression valueParameter = Expression.Parameter(typeof(object), "value");
            Expression instanceCast = Expression.Convert(instanceParameter, property.DeclaringType);
            Expression valueCast = Expression.Convert(valueParameter, entityType);
            MemberExpression propertyAccessor = Expression.Property(instanceCast, property);
            
            Func<object, object> compiledValueGetter = null;
            if (!isCollection)
            {
                Expression propertyValueCast = Expression.Convert(propertyAccessor, typeof(object));
                Expression<Func<object, object>> getValueLambda = Expression.Lambda<Func<object, object>>(propertyValueCast, instanceParameter);
                compiledValueGetter = getValueLambda.Compile();
            }

            Action<object, object> compiledValueSetter = null;
            if (property.CanWrite)
            {
                Expression setValue = valueSetter(propertyAccessor, valueCast);
                Expression<Action<object, object>> setValueLambda = Expression.Lambda<Action<object, object>>(setValue, instanceParameter, valueParameter);
                compiledValueSetter = setValueLambda.Compile();
            }
            
            return new EntityProperty(property.Name, entityType, isCollection, compiledValueGetter, compiledValueSetter);
        }

        private static bool TryGetCollectionType(Type type, out Type collectionType)
        {
            // No collection logic is applied to byte arrays as they are simply being set to a binary
            if (type == typeof(byte[]))
            {
                collectionType = null;
                return false;
            }

            // Direct ICollection<T> property
            if (IsCollectionType(type))
            {
                collectionType = type;
                return true;
            }

            // Indirect ICollection<T> property, i.E.: IList<T>
            Type baseCollectionType = type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(IsCollectionType);
            if (baseCollectionType != null)
            {
                collectionType = baseCollectionType;
                return true;
            }

            // No collection property
            collectionType = null;
            return false;
        }

        private static bool IsCollectionType(Type type) => type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>);

        private static ObfuscatedProperty BuildObfuscatedPropertyAccessor(PropertyInfo propertyInfo)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
            Expression instance = Expression.Convert(instanceParameter, propertyInfo.DeclaringType);
            Expression property = Expression.Property(instance, propertyInfo);
            Expression deobfuscator = Expression.Call(typeof(TextObfuscator), nameof(TextObfuscator.Deobfuscate), new Type[0], property);
            Expression assign = Expression.Assign(property, deobfuscator);
            Expression<Action<object>> expression = Expression.Lambda<Action<object>>(assign, instanceParameter);
            Action<object> compiled = expression.Compile();
            return new ObfuscatedProperty(compiled);
        }
    }
}