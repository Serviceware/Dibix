using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace Dibix.Sdk.Sql
{
    public static class DacMetadataManager
    {
        private static readonly Func<string, bool> IsEmbeddedMethod = CompileIsEmbeddedMethod();
        private static readonly Action<FileInfo> WriteIsEmbeddedMethod = CompileWriteIsEmbeddedMethod();

        public static bool IsEmbedded(string packagePath)
        {
            bool isEmbedded = IsEmbeddedMethod(packagePath);
            return isEmbedded;
        }

        public static void SetIsEmbedded(string packagePath)
        {
            if (!File.Exists(packagePath))
                throw new FileNotFoundException(null, packagePath);

            WriteIsEmbeddedMethod(new FileInfo(packagePath));
        }

        private static Func<string, bool> CompileIsEmbeddedMethod()
        {
            // SqlPackage package;
            // Stream stream;
            // try
            // {
            //     package = SqlPackage.Open(path);
            //     SqlPackageContent content = package.GetContent(SqlPackageContentType.Model);
            //     stream = content.GetStream();
            //     bool isEmbedded = DacMetadataManager.IsEmbedded(stream));
            //     return isEmbedded;
            // }
            // finally
            // {
            //     if (stream != null)
            //         stream.Dispose();
            //
            //     if (package != null)
            //         package.Dispose();
            // }

            // (string file) => 
            ParameterExpression fileParameter = Expression.Parameter(typeof(string), "file");

            Func<string, bool> compiled = CompileMethod<Func<string, bool>>
            (
                parameter: fileParameter
              , packageOpener: (sqlPackageType, parameter) => Expression.Call(sqlPackageType, "Open", new Type[0], parameter, Expression.Constant(FileAccess.Read))
              , body: (streamVariable, parameters, statements) =>
              {
                  ParameterExpression isEmbeddedVariable = Expression.Parameter(typeof(bool), "isEmbedded");
                  Expression isEmbeddedValue = Expression.Call(typeof(DacMetadataManager), nameof(IsEmbedded), new Type[0], streamVariable);
                  Expression isEmbeddedAssign = Expression.Assign(isEmbeddedVariable, isEmbeddedValue);
                  parameters.Add(isEmbeddedVariable);
                  statements.Add(isEmbeddedAssign);
              }
            );
            return compiled;
        }

        private static bool IsEmbedded(Stream stream)
        {
            (XDocument _, XElement element, XName _) = GetIsEmbeddedElement(stream);
            bool isEmbedded = String.Equals(element?.Value, Boolean.TrueString, StringComparison.OrdinalIgnoreCase);
            return isEmbedded;
        }

        private static Action<FileInfo> CompileWriteIsEmbeddedMethod()
        {
            // SqlPackage package;
            // Stream stream;
            // try
            // {
            //     package = SqlPackage.OpenForUpdate(fileInfo);
            //     stream = content.GetStream();
            //     DacMetadataManager.WriteIsEmbedded(stream);
            // }
            // finally
            // {
            //     if (stream != null)
            //         stream.Dispose();
            //
            //     if (package != null)
            //         package.Dispose();
            // }

            // (FileInfo fileInfo) => 
            ParameterExpression fileInfoParameter = Expression.Parameter(typeof(FileInfo), "fileInfo");

            Action<FileInfo> compiled = CompileMethod<Action<FileInfo>>
            (
                parameter: fileInfoParameter
              , packageOpener: (sqlPackageType, parameter) => Expression.Call(sqlPackageType, "OpenForUpdate", new Type[0], parameter)
              , body: (streamVariable, parameters, statements) =>
              {
                  Expression writeIsEmbeddedCall = Expression.Call(typeof(DacMetadataManager), nameof(WriteIsEmbedded), new Type[0], streamVariable);
                  statements.Add(writeIsEmbeddedCall);
              }
            );
            return compiled;
        }

        private static void WriteIsEmbedded(Stream stream)
        {
            (XDocument document, XElement element, XName elementName) = GetIsEmbeddedElement(stream);
            if (element == null)
            {
                element = new XElement(elementName);
                document.Root.Add(element);
            }
            element.Value = Boolean.TrueString;
            stream.Position = 0;
            document.Save(stream);
        }

        private static TDelegate CompileMethod<TDelegate>
        (
            ParameterExpression parameter
          , Func<Type, Expression, Expression> packageOpener
          , Action<Expression, ICollection<ParameterExpression>, ICollection<Expression>> body
        )
        {
            // package = <packageOpener>(parameter);
            Type sqlPackageType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.Build.SqlPackage", true);
            ParameterExpression packageVariable = Expression.Variable(sqlPackageType, "package");
            Expression packageValue = packageOpener(sqlPackageType, parameter);
            Expression packageAssign = Expression.Assign(packageVariable, packageValue);

            // SqlPackageContent content = package.GetContent(SqlPackageContentType.DacOrigin);
            Type sqlPackageContentType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.Build.SqlPackageContent", true);
            Type sqlPackageContentTypeType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.Build.SqlPackageContentType", true);
            ParameterExpression contentVariable = Expression.Variable(sqlPackageContentType, "content");
            object sqlPackageContentTypeValue = Enum.Parse(sqlPackageContentTypeType, "DacOrigin");
            Expression sqlPackageContentTypeModelParameter = Expression.Constant(sqlPackageContentTypeValue, sqlPackageContentTypeType);
            Expression contentValue = Expression.Call(packageVariable, "GetContent", new Type[0], sqlPackageContentTypeModelParameter);
            Expression contentAssign = Expression.Assign(contentVariable, contentValue);

            // stream = content.GetStream();
            ParameterExpression streamVariable = Expression.Variable(typeof(Stream), "stream");
            Expression streamValue = Expression.Call(contentVariable, "GetStream", new Type[0]);
            Expression streamAssign = Expression.Assign(streamVariable, streamValue);

            // <body>
            IList<ParameterExpression> parameters = new Collection<ParameterExpression>();
            IList<Expression> statements = new Collection<Expression>();
            body(streamVariable, parameters, statements);
            parameters.Insert(0, contentVariable);
            statements.Insert(0, packageAssign);
            statements.Insert(1, contentAssign);
            statements.Insert(2, streamAssign);

            // if (stream != null)
            //     stream.Dispose();
            MethodInfo disposeMethod = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose));
            Expression disposeStream = Expression.Call(streamVariable, disposeMethod);
            Expression @null = Expression.Constant(null);
            Expression disposeStreamIf = Expression.IfThen(Expression.NotEqual(streamVariable, @null), disposeStream);

            // if (package != null)
            //     package.Dispose();
            Expression disposePackage = Expression.Call(packageVariable, disposeMethod);
            Expression disposePackageIf = Expression.IfThen(Expression.NotEqual(packageVariable, @null), disposePackage);

            // try
            // {
            //     ..
            // }
            // finally
            // {
            //     ..
            // }
            Expression tryBlock = Expression.Block(parameters, statements);
            Expression @finally = Expression.Block(disposeStreamIf, disposePackageIf);
            Expression tryFinally = Expression.TryFinally(tryBlock, @finally);


            Expression block = Expression.Block
            (
                new[]
                {
                    packageVariable
                  , streamVariable
                }
              , tryFinally
            );
            Expression<TDelegate> lambda = Expression.Lambda<TDelegate>
            (
                block
              , parameter
            );
            TDelegate compiled = lambda.Compile();
            return compiled;
        }

        private static (XDocument document, XElement element, XName elementName) GetIsEmbeddedElement(Stream stream)
        {
            XDocument document = XDocument.Load(stream);
            XNamespace ns = "http://schemas.microsoft.com/sqlserver/dac/Serialization/2012/02";
            XName elementName = ns + "IsEmbedded";
            XElement element = document.Root.Element(elementName);
            return (document, element, elementName);
        }
    }
}