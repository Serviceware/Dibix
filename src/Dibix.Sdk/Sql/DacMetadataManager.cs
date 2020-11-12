using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;

namespace Dibix.Sdk.Sql
{
    public static class DacMetadataManager
    {
        private static readonly Func<string, bool?> IsEmbeddedMethod = CompileIsEmbeddedMethod();
        private static readonly Action<FileInfo, bool> WriteIsEmbeddedMethod = CompileWriteIsEmbeddedMethod();

        public static bool? IsEmbedded(string packagePath)
        {
            bool? isEmbedded = IsEmbeddedMethod(packagePath);
            return isEmbedded;
        }

        public static void SetIsEmbedded(string packagePath, bool value)
        {
            if (!File.Exists(packagePath))
                throw new FileNotFoundException(null, packagePath);

            int tries = 0;
            while(true)
            {
                try
                {
                    WriteIsEmbeddedMethod(new FileInfo(packagePath), value);
                    break;
                }
                catch (Exception exception) when (exception.InnerException is IOException)
                {
                    // When trying to open the dac file for writing we sometimes get:
                    // System.IO.IOException: The process cannot access the file '' because it is being used by another process.
                    // Since the dac file is opened for writing immediately after the SqlBuild target is completed, it might be that it is not fully closed yet. (Asynchronicity)
                    if (++tries == 10)
                        throw;
                    
                    Thread.Sleep(1000);
                }
            }
        }

        private static Func<string, bool?> CompileIsEmbeddedMethod()
        {
            // SqlPackage package;
            // Stream stream;
            // try
            // {
            //     package = SqlPackage.Open(path);
            //     SqlPackageContent content = package.GetContent(SqlPackageContentType.Model);
            //     stream = content.GetStream();
            //     bool? isEmbedded = DacMetadataManager.IsEmbedded(stream));
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

            Func<string, bool?> compiled = CompileMethod<Func<string, bool?>>
            (
                packageOpener: sqlPackageType => Expression.Call(sqlPackageType, "Open", new Type[0], fileParameter, Expression.Constant(FileAccess.Read))
              , body: (streamVariable, parameters, statements) =>
                {
                    ParameterExpression isEmbeddedVariable = Expression.Parameter(typeof(bool?), "isEmbedded");
                    Expression isEmbeddedValue = Expression.Call(typeof(DacMetadataManager), nameof(IsEmbedded), new Type[0], streamVariable);
                    Expression isEmbeddedAssign = Expression.Assign(isEmbeddedVariable, isEmbeddedValue);
                    parameters.Add(isEmbeddedVariable);
                    statements.Add(isEmbeddedAssign);
                }
              , fileParameter
            );
            return compiled;
        }

        private static bool? IsEmbedded(Stream stream)
        {
            (XDocument _, XElement element, XName _) = GetIsEmbeddedElement(stream);
            bool? isEmbedded = Boolean.TryParse(element?.Value, out bool isEmbeddedValue) ? isEmbeddedValue : (bool?)null;
            return isEmbedded;
        }

        private static Action<FileInfo, bool> CompileWriteIsEmbeddedMethod()
        {
            // SqlPackage package;
            // Stream stream;
            // try
            // {
            //     package = SqlPackage.OpenForUpdate(fileInfo);
            //     stream = content.GetStream();
            //     DacMetadataManager.WriteIsEmbedded(stream, value);
            // }
            // finally
            // {
            //     if (stream != null)
            //         stream.Dispose();
            //
            //     if (package != null)
            //         package.Dispose();
            // }

            // (FileInfo fileInfo, bool value) => 
            ParameterExpression fileInfoParameter = Expression.Parameter(typeof(FileInfo), "fileInfo");
            ParameterExpression valueParameter = Expression.Parameter(typeof(bool), "value");

            Action<FileInfo, bool> compiled = CompileMethod<Action<FileInfo, bool>>
            (
                packageOpener: sqlPackageType => Expression.Call(sqlPackageType, "OpenForUpdate", new Type[0], fileInfoParameter)
              , body: (streamVariable, parameters, statements) =>
                {
                    Expression writeIsEmbeddedCall = Expression.Call(typeof(DacMetadataManager), nameof(WriteIsEmbedded), new Type[0], streamVariable, valueParameter);
                    statements.Add(writeIsEmbeddedCall);
                }
              , fileInfoParameter
              , valueParameter
            );
            return compiled;
        }

        private static void WriteIsEmbedded(Stream stream, bool value)
        {
            (XDocument document, XElement element, XName elementName) = GetIsEmbeddedElement(stream);
            if (element == null)
            {
                element = new XElement(elementName);
                document.Root.Add(element);
            }

            string elementValue = value.ToString();
            if (element.Value == elementValue) 
                return;

            element.Value = elementValue;
            stream.Position = 0;
            document.Save(stream);
        }

        private static TDelegate CompileMethod<TDelegate>
        (
            Func<Type, Expression> packageOpener
          , Action<Expression, ICollection<ParameterExpression>, ICollection<Expression>> body
          , params ParameterExpression[] parameters
        )
        {
            // package = <packageOpener>(parameter);
            Type sqlPackageType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.Build.SqlPackage", true);
            ParameterExpression packageVariable = Expression.Variable(sqlPackageType, "package");
            Expression packageValue = packageOpener(sqlPackageType);
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
            IList<ParameterExpression> bodyParameters = new Collection<ParameterExpression>();
            IList<Expression> bodyStatements = new Collection<Expression>();
            body(streamVariable, bodyParameters, bodyStatements);
            bodyParameters.Insert(0, contentVariable);
            bodyStatements.Insert(0, packageAssign);
            bodyStatements.Insert(1, contentAssign);
            bodyStatements.Insert(2, streamAssign);

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
            Expression tryBlock = Expression.Block(bodyParameters, bodyStatements);
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
              , parameters
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