using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Dibix.Sdk.Sql
{
    public static class DacCustomDataHeaderReader
    {
        private static readonly Func<string, bool> IsEmbeddedMethod = CompileIsEmbeddedMethod();

        public static bool IsEmbedded(string file)
        {
            bool isEmbedded = IsEmbeddedMethod(file);
            return isEmbedded;
        }

        private static Func<string, bool> CompileIsEmbeddedMethod()
        {
            // bool result;
            // SqlPackage package;
            // Stream stream;
            // StreamReader reader;
            // IEnumerator<CustomSchemaData> customDataEnumerator;
            // try
            // {
            //     package = SqlPackage.Open(path);
            //     SqlPackageContent content = package.GetContent(SqlPackageContentType.Model);
            //     stream = content.GetStream();
            //     reader = new StreamReader(stream);
            //     DataSchemaModelHeader header = DataSchemaModel.ReadDataSchemaModelHeaderFromXml(reader, readCustomData: true);
            //     customDataEnumerator = header.CustomData.GetEnumerator();
            //     while (customDataEnumerator.MoveNext())
            //     {
            //         CustomSchemaData customDataElement = customDataEnumerator.Current;
            //         if (customDataElement.MatchesTypeAndCategory("SqlCmdVariables", "SqlCmdVariable") && customDataElement.GetMetadata("IsEmbedded") != null)
            //         {
            //             result = true;
            //             break;
            //         }
            //     }
            // }
            // finally
            // {
            //     if (customDataEnumerator != null)
            //         customDataEnumerator.Dispose();
            //
            //     if (reader != null)
            //         reader.Dispose();
            //
            //     if (stream != null)
            //         stream.Dispose();
            //
            //     if (package != null)
            //         package.Dispose();
            // }
            // return result;

            // (string file) => 
            ParameterExpression fileParameter = Expression.Parameter(typeof(string), "file");

            // package = SqlPackage.Open(path);
            Type sqlPackageType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.Build.SqlPackage", true);
            ParameterExpression packageVariable = Expression.Variable(sqlPackageType, "package");
            Expression packageValue = Expression.Call(sqlPackageType, "Open", new Type[0], fileParameter, Expression.Constant(FileAccess.Read));
            Expression packageAssign = Expression.Assign(packageVariable, packageValue);

            // SqlPackageContent content = package.GetContent(SqlPackageContentType.Model);
            Type sqlPackageContentType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.Build.SqlPackageContent", true);
            Type sqlPackageContentTypeType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.Build.SqlPackageContentType", true);
            ParameterExpression contentVariable = Expression.Variable(sqlPackageContentType, "content");
            object sqlPackageContentTypeModel = Enum.Parse(sqlPackageContentTypeType, "Model");
            Expression sqlPackageContentTypeModelParameter = Expression.Constant(sqlPackageContentTypeModel, sqlPackageContentTypeType);
            Expression contentValue = Expression.Call(packageVariable, "GetContent", new Type[0], sqlPackageContentTypeModelParameter);
            Expression contentAssign = Expression.Assign(contentVariable, contentValue);

            // stream = content.GetStream();
            ParameterExpression streamVariable = Expression.Variable(typeof(Stream), "stream");
            Expression streamValue = Expression.Call(contentVariable, "GetStream", new Type[0]);
            Expression streamAssign = Expression.Assign(streamVariable, streamValue);

            // streamReader = new StreamReader(stream);
            ParameterExpression readerVariable = Expression.Variable(typeof(StreamReader), "streamReader");
            ConstructorInfo streamReaderCtor = typeof(StreamReader).GetConstructor(new[] { typeof(Stream) });
            if (streamReaderCtor == null)
                throw new InvalidOperationException("Could not find constructor: new StreamReader(Stream)");

            Expression readerValue = Expression.New(streamReaderCtor, streamVariable);
            Expression readerAssign = Expression.Assign(readerVariable, readerValue);

            // DataSchemaModelHeader header = DataSchemaModel.ReadDataSchemaModelHeaderFromXml(streamReader, readCustomData: true);
            Type dataSchemaModelHeaderType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.SchemaModel.DataSchemaModelHeader", true);
            ParameterExpression headerVariable = Expression.Variable(dataSchemaModelHeaderType, "header");
            Expression readCustomData = Expression.Constant(true);
            Type dataSchemaModelType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.SchemaModel.DataSchemaModel", true);
            Expression headerValue = Expression.Call(dataSchemaModelType, "ReadDataSchemaModelHeaderFromXml", new Type[0], readerVariable, readCustomData);
            Expression headerAssign = Expression.Assign(headerVariable, headerValue);

            // customDataEnumerator = header.CustomData.GetEnumerator();
            Type customSchemaDataType = DacReflectionUtility.SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.SchemaModel.CustomSchemaData", true);
            ParameterExpression customDataEnumeratorVariable = Expression.Variable(typeof(IEnumerator<>).MakeGenericType(customSchemaDataType), "customDataEnumerator");
            Expression customDataProperty = Expression.Property(headerVariable, "CustomData");
            MethodInfo getEnumeratorMethod = typeof(IEnumerable<>).MakeGenericType(customSchemaDataType).GetMethod(nameof(IEnumerable<object>.GetEnumerator));
            Expression customDataEnumeratorValue = Expression.Call(customDataProperty, getEnumeratorMethod);
            Expression customDataEnumeratorAssign = Expression.Assign(customDataEnumeratorVariable, customDataEnumeratorValue);

            // CustomSchemaData customDataElement = customDataEnumerator.Current;
            ParameterExpression customDataElementVariable = Expression.Variable(customSchemaDataType, "customDataElement");
            Expression customDataElementValue = Expression.Property(customDataEnumeratorVariable, nameof(IEnumerator<object>.Current));
            Expression customDataElementAssign = Expression.Assign(customDataElementVariable, customDataElementValue);


            LabelTarget loopBreakLabel = Expression.Label("LoopBreak");
            Expression loopBreak = Expression.Break(loopBreakLabel);

            // if (customDataElement.MatchesTypeAndCategory("SqlCmdVariables", "SqlCmdVariable") && customDataElement.GetMetadata("IsEmbedded") != null)
            //     return true;
            ParameterExpression resultVariable = Expression.Variable(typeof(bool), "result");
            Expression matchesTypeAndCategoryCall = Expression.Call(customDataElementVariable, "MatchesTypeAndCategory", new Type[0], Expression.Constant("SqlCmdVariables"), Expression.Constant("SqlCmdVariable"));
            Expression getMetadataCall = Expression.Call(customDataElementVariable, "GetMetadata", new Type[0], Expression.Constant("IsEmbedded"));
            Expression matchesTypeAndCategoryIf = Expression.IsTrue(matchesTypeAndCategoryCall);
            Expression @null = Expression.Constant(null);
            Expression getMetadataIf = Expression.NotEqual(getMetadataCall, @null);
            Expression isEmbeddedCondition = Expression.And(matchesTypeAndCategoryIf, getMetadataIf);
            Expression resultAssign = Expression.Assign(resultVariable, Expression.Constant(true));
            Expression isEmbeddedThen = Expression.Block(resultAssign, loopBreak);
            Expression isEmbeddedIf = Expression.IfThen(isEmbeddedCondition, isEmbeddedThen);

            // while (customDataEnumerator.MoveNext())
            // {
            //     ..
            // }
            Expression itemBlock = Expression.Block(new[] { customDataElementVariable }, customDataElementAssign, isEmbeddedIf);
            Expression moveNextCall = Expression.Call(customDataEnumeratorVariable, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext)));
            Expression loopCondition = Expression.IsTrue(moveNextCall);
            Expression loopConditionBlock = Expression.IfThenElse(loopCondition, itemBlock, loopBreak);
            Expression loop = Expression.Loop(loopConditionBlock, loopBreakLabel);

            // if (customDataEnumerator != null)
            //     customDataEnumerator.Dispose();
            MethodInfo disposeMethod = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose));
            Expression disposeCustomDataEnumerator = Expression.Call(customDataEnumeratorVariable, disposeMethod);
            Expression disposeCustomDataEnumeratorIf = Expression.IfThen(Expression.NotEqual(customDataEnumeratorVariable, @null), disposeCustomDataEnumerator);
            
            // if (reader != null)
            //     reader.Dispose();
            Expression disposeReader = Expression.Call(readerVariable, disposeMethod);
            Expression disposeReaderIf = Expression.IfThen(Expression.NotEqual(readerVariable, @null), disposeReader);

            // if (stream != null)
            //     stream.Dispose();
            Expression disposeStream = Expression.Call(streamVariable, disposeMethod);
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
            Expression tryBlock = Expression.Block
            (
                new[]
                {
                    contentVariable
                  , headerVariable
                }
              , packageAssign
              , contentAssign
              , streamAssign
              , readerAssign
              , headerAssign
              , customDataEnumeratorAssign
              , loop
            );
            Expression @finally = Expression.Block(disposeCustomDataEnumeratorIf, disposeReaderIf, disposeStreamIf, disposePackageIf);
            Expression tryFinally = Expression.TryFinally(tryBlock, @finally);


            Expression block = Expression.Block
            (
                new[]
                {
                    resultVariable
                  , packageVariable
                  , streamVariable
                  , readerVariable
                  , customDataEnumeratorVariable
                }
              , tryFinally
              , resultVariable
            );
            Expression<Func<string, bool>> lambda = Expression.Lambda<Func<string, bool>>
            (
                block
              , fileParameter
            );
            Func<string, bool> compiled = lambda.Compile();
            return compiled;
        }
    }
}