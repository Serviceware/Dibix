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
                else if (!property.PropertyType.IsPrimitive())
                {
                    EntityProperty entityProperty = BuildEntityProperty(property);
                    descriptor.ComplexProperties.Add(entityProperty);
                }
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
                return BuildComplexEntityProperty(property);

            return BuildCollectionEntityProperty(property, collectionType);
        }

        private static EntityProperty BuildComplexEntityProperty(PropertyInfo property)
        {
            Type entityType = property.PropertyType;
            return BuildEntityProperty(property, entityType, true, Expression.Assign);
        }

        private static EntityProperty BuildCollectionEntityProperty(PropertyInfo propertyInfo, Type collectionType)
        {
            Type entityType = propertyInfo.PropertyType.GenericTypeArguments[0];
            return BuildEntityProperty(propertyInfo, entityType, true, (property, value) => Expression.Call(property, collectionType.GetTypeInfo().GetDeclaredMethod("Add"), value));
        }

        private static EntityProperty BuildEntityProperty(PropertyInfo property, Type entityType, bool isCollection, Func<Expression, Expression, Expression> valueSetter)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
            ParameterExpression valueParameter = Expression.Parameter(typeof(object), "value");
            Expression instanceCast = Expression.Convert(instanceParameter, property.DeclaringType);
            Expression valueCast = Expression.Convert(valueParameter, entityType);
            Expression propertyAccessor = Expression.Property(instanceCast, property);
            Expression setValue = valueSetter(propertyAccessor, valueCast);
            Expression<Action<object, object>> expression = Expression.Lambda<Action<object, object>>(setValue, instanceParameter, valueParameter);
            Action<object, object> compiled = expression.Compile();
            return new EntityProperty(property.Name, entityType, isCollection, compiled);
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
    }
}