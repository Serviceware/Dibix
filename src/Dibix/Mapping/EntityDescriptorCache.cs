using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace Dibix
{
    internal static class EntityDescriptorCache
    {
        private static readonly ConcurrentDictionary<Type, EntityDescriptor> DescriptorCache = new ConcurrentDictionary<Type, EntityDescriptor>();
        private static readonly IEntityPropertyFormatter[] PropertyFormatters =
        {
            new DateTimeKindEntityPropertyFormatter()
          , new TextObfuscationEntityPropertyFormatter()
        };

        public static EntityDescriptor GetDescriptor(Type type) => DescriptorCache.GetOrAdd(type, BuildDescriptor);

        private static EntityDescriptor BuildDescriptor(Type type)
        {
            EntityDescriptor descriptor = new EntityDescriptor();
            if (type.IsPrimitive() || type == typeof(byte[]) || type == typeof(XElement))
                return descriptor;

            IDictionary<PropertyInfo, ICollection<IEntityPropertyFormatter>> formattableProperties = new Dictionary<PropertyInfo, ICollection<IEntityPropertyFormatter>>();
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
                else if (!IsPropertySupported(property))
                {
                    continue;
                }
                else
                {
                    EntityProperty entityProperty = BuildEntityProperty(property);
                    descriptor.Properties.Add(entityProperty);
                }

                foreach (IEntityPropertyFormatter propertyFormatter in PropertyFormatters.Where(x => x.RequiresFormatting(property)))
                {
                    if (!formattableProperties.TryGetValue(property, out ICollection<IEntityPropertyFormatter> formatters))
                    {
                        formatters = new Collection<IEntityPropertyFormatter>();
                        formattableProperties.Add(property, formatters);
                    }
                    formatters.Add(propertyFormatter);
                }
            }

            if (descriptor.Discriminator != null && descriptor.Keys.Count != 1)
                throw new InvalidOperationException($"To match a discriminator, exactly one key property should be defined: {type}");

            if (formattableProperties.Any())
                descriptor.InitPostProcessor(CompilePostProcessor(type, formattableProperties));

            return descriptor;
        }

        private static bool IsPropertySupported(PropertyInfo property)
        {
            // Indexers are currently not supported
            if (property.GetIndexParameters().Any())
                return false;

            return true;
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
            return BuildEntityProperty(property, entityType, false, property.CanWrite ? Expression.Assign : (Func<Expression, Expression, Expression>)null);
        }

        private static EntityProperty BuildCollectionEntityProperty(PropertyInfo propertyInfo, Type collectionType)
        {
            Type entityType = collectionType.GenericTypeArguments[0];
            return BuildEntityProperty(propertyInfo, entityType, true, (property, value) => Expression.Call(property, collectionType.SafeGetMethod("Add"), value));
        }

        private static EntityProperty BuildEntityProperty(PropertyInfo property, Type entityType, bool isCollection, Func<Expression, Expression, Expression> valueSetter)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
            ParameterExpression valueParameter = Expression.Parameter(typeof(object), "value");
            Expression instanceCast = Expression.Convert(instanceParameter, property.DeclaringType);
            MemberExpression propertyAccessor = Expression.Property(instanceCast, property);

            Expression propertyValueCast = Expression.Convert(propertyAccessor, typeof(object));
            Expression<Func<object, object>> valueGetterLambda = Expression.Lambda<Func<object, object>>(propertyValueCast, instanceParameter);
            Func<object, object> compiledValueGetter = valueGetterLambda.Compile();

            Action<object, object> compiledValueSetter = null;
            if (valueSetter != null)
            {
                Expression valueCast = Expression.Convert(valueParameter, entityType);
                Expression setValue = valueSetter(propertyAccessor, valueCast);
                Expression<Action<object, object>> valueSetterLambda = Expression.Lambda<Action<object, object>>(setValue, instanceParameter, valueParameter);
                compiledValueSetter = valueSetterLambda.Compile();
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
            Type baseCollectionType = type.GetInterfaces().FirstOrDefault(IsCollectionType);
            if (baseCollectionType != null)
            {
                collectionType = baseCollectionType;
                return true;
            }

            // No collection property
            collectionType = null;
            return false;
        }

        private static bool IsCollectionType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>);

        private static Delegate CompilePostProcessor(Type type, IDictionary<PropertyInfo, ICollection<IEntityPropertyFormatter>> formattableProperties)
        {
            ParameterExpression instanceParameter = Expression.Parameter(type, "instance");

            IEnumerable<Expression> statements = from formattableProperty in formattableProperties
                                                 let propertyInfo = formattableProperty.Key
                                                 from formatter in formattableProperty.Value
                                                 let propertyExpression = Expression.Property(instanceParameter, propertyInfo)
                                                 select Expression.Assign(propertyExpression, formatter.BuildExpression(propertyInfo, propertyExpression));

            Expression block = Expression.Block(statements);
            LambdaExpression lambda = Expression.Lambda(block, instanceParameter);
            Delegate compiled = lambda.Compile();
            return compiled;
        }
    }
}